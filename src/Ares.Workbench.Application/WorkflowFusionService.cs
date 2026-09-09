using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Ares.Workbench.Domain;
namespace Ares.Workbench.Application;

public sealed partial class WorkbenchService
{
    private static string ProjectFingerprint(ProjectProfile p)=>Convert.ToHexString(SHA256.HashData(
        Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new {p.RepoRoot,p.AllowedPaths,p.InstructionFiles,p.BuildCommand,p.TestCommand}))));
    private void SaveFusion(EngineeringTask task,FusionTask fusion,TaskLifecycle? state=null) =>
        ((ITaskStore)store).Update(task with {Fusion=fusion with {Revision=fusion.Revision+1},Lifecycle=state??task.Lifecycle,UpdatedAt=DateTimeOffset.UtcNow});
    private EngineeringTask EditableDiscussion(string id,long revision)
    {
        var task=store.Task(id);
        if(task.Lifecycle!=TaskLifecycle.Discussing || task.Fusion is not {} f || f.Revision!=revision ||
            f.DiscussionState is "Queued" or "Running")
            throw new InvalidOperationException("讨论状态已改变或正在回复；请刷新后重试。Ready 后的约束不可编辑。");
        return task;
    }
    public EngineeringTask CreateDiscussingTask(string projectId,string title,string goal,
        IEnumerable<string> acceptance,IEnumerable<string> constraints,Risk risk)
    {
        lock(sync) {
            var p=workspaces.Validate(store.Project(projectId));
            if(string.IsNullOrWhiteSpace(title)||!Enum.IsDefined(risk))throw new ArgumentException("请填写任务标题并选择工作流。");
            var now=DateTimeOffset.UtcNow;var a=acceptance.Where(x=>!string.IsNullOrWhiteSpace(x)).Select(x=>x.Trim()).ToImmutableArray();
            var cs=constraints.Where(x=>!string.IsNullOrWhiteSpace(x)).ToImmutableArray();
            var task=new EngineeringTask(Guid.NewGuid().ToString("N"),title.Trim(),(goal??"").Trim(),a,cs,risk,p.ProjectId,TaskLifecycle.Discussing,now,now) {
                Fusion=new(new((goal??"").Trim(),a,"",string.Join("\n",cs),"","",[]),[],[])
            };
            ((ITaskStore)store).Add(task);return task;
        }
    }
    public void RequestDiscussion(string id,long revision,string message)
    {
        lock(sync) {
            var task=EditableDiscussion(id,revision);
            if(string.IsNullOrWhiteSpace(message)||message.Length>16000)throw new ArgumentException("请输入 1–16000 字的讨论消息。");
            if(ProjectBusy(task.WorkspaceId))throw new InvalidOperationException("项目已有讨论、执行或审批，请完成后再发送。");
            var f=task.Fusion!;
            SaveFusion(task,f with {DiscussionState="Queued",Error=null,Turns=f.Turns.Add(new("Owner",message.Trim(),DateTimeOffset.UtcNow))});
            queue.Enqueue(new(id,Discuss:true));
        }
    }
    public void StopDiscussion(string id)
    {
        lock(sync) {
            var task=store.Task(id);var f=task.Fusion??throw new InvalidOperationException();
            if(executing.TryGetValue(id,out var c)){c.Cancellation.Cancel();return;}
            if(f.DiscussionState!="Queued")throw new InvalidOperationException("讨论当前未运行。");
            SaveFusion(task,f with {DiscussionState="Blocked",Error="Owner 停止了排队讨论，可发送新消息继续。"});
        }
    }
    private async Task ProcessDiscussionAsync(string id,CancellationToken ct)
    {
        EngineeringTask task;using var linked=CancellationTokenSource.CreateLinkedTokenSource(ct);
        lock(sync) {
            task=store.Task(id);
            if(task.Fusion?.DiscussionState!="Queued")return;
            SaveFusion(task,task.Fusion with {DiscussionState="Running"});
            task=store.Task(id);executing.Add(id,new(linked));
        }
        try {
            var project=workspaces.Validate(store.Project(task.WorkspaceId));
            var workspace=workspaces.Create(project,id);
            var stamp=await handlers.WorkspaceStampAsync(project,workspace,linked.Token);
            var old=store.Checkpoint(id);
            if(old is not null && !string.Equals(old.RepoRoot,project.RepoRoot,StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("原生讨论绑定的仓库根目录已改变，请新建任务。");
            store.SaveCheckpoint(new(id,RunState.Running,"discuss",null,null,null,[],0,
                task.Fusion!.PrimarySessionRef??old?.PrimarySessionRef,null,DateTimeOffset.UtcNow,ProjectFingerprint(project),project.RepoRoot,stamp));
            // This immutable invocation is an adapter input, not a second Run or a native session implementation.
            var now=DateTimeOffset.UtcNow;
            var run=new WorkflowRun(id,id,"DISCUSSION",1,backend.BackendId,RunState.Running,"discuss",0,now,now,0,[],null,null);
            var node=new NodeDefinition("discuss",NodeKind.Agent,"context/v1","node-result/v1",SideEffect.ReadOnly,TimeSpan.FromMinutes(10),new(0),"discuss");
            var parts=handlers.Create(project,workspace);
            var context=await parts.Contexts.BuildAsync(task,workspace,run,linked.Token);
            var result=await parts.Handlers["discuss"].ExecuteAsync(new(node,task,run,workspace,context),linked.Token);
            var session=store.Checkpoint(id)?.PrimarySessionRef;
            lock(sync) {
                task=store.Task(id);var f=task.Fusion!;
                if(result.Outcome!=NodeOutcome.Succeeded) {
                    SaveFusion(task,f with {PrimarySessionRef=session??f.PrimarySessionRef,DiscussionState="Blocked",Error=result.Failure?.Message??"讨论已中断，可发送下一条消息续接。"});return;
                }
                if(!Guid.TryParse(session,out _) || f.PrimarySessionRef is not null && f.PrimarySessionRef!=session)
                    throw new InvalidOperationException("未确认同一个 Primary 原生 session，讨论不能进入 Ready。");
                using var doc=JsonDocument.Parse(result.Output);
                var reply=doc.RootElement.GetProperty("content").GetString()??"";
                var draft=doc.RootElement.GetProperty("ready").Deserialize<ReadyAnchors>()??throw new InvalidDataException("Ready proposal missing.");
                if(draft.Acceptance.IsDefault||draft.Unresolved.IsDefault||string.IsNullOrWhiteSpace(reply))throw new InvalidDataException("讨论输出不完整。");
                SaveFusion(task,f with {PrimarySessionRef=session,Draft=draft,DiscussionState="Idle",Error=null,
                    Turns=f.Turns.Add(new("Primary Codex",reply,DateTimeOffset.UtcNow))});
            }
        } catch(Exception ex) {
            lock(sync) {
                task=store.Task(id);var f=task.Fusion!;
                SaveFusion(task,f with {PrimarySessionRef=store.Checkpoint(id)?.PrimarySessionRef??f.PrimarySessionRef,
                    DiscussionState="Blocked",Error=ex is OperationCanceledException?"讨论已中断；可发送新消息续接已记录的原生 session。":ex.Message});
            }
        } finally {lock(sync)executing.Remove(id);}
    }
    public void SaveReadyDraft(string id,long revision,ReadyAnchors anchors)
    {
        lock(sync) {
            var task=EditableDiscussion(id,revision);
            SaveFusion(task,task.Fusion! with {Draft=anchors});
        }
    }
    public void FreezeReady(string id,long revision,ReadyAnchors anchors)
    {
        lock(sync) {
            var task=EditableDiscussion(id,revision);var f=task.Fusion!;
            anchors.Validate();
            var project=workspaces.Validate(store.Project(task.WorkspaceId));
            if(ProjectBusy(project.ProjectId))throw new InvalidOperationException("项目忙，请稍后 Ready。");
            var cp=store.Checkpoint(id);
            if(f.DiscussionState!="Idle" || !Guid.TryParse(f.PrimarySessionRef,out _) || !f.Turns.Any(t=>t.Speaker=="Primary Codex") ||
                cp is null || cp.PrimarySessionRef!=f.PrimarySessionRef || cp.TaskFingerprint!=ProjectFingerprint(project))
                throw new InvalidOperationException("请先与 Primary 完成一次成功讨论；项目配置变化后需要再次讨论。");
            var snapshot=new ReadySnapshot(f.Snapshots.Length+1,anchors,DateTimeOffset.UtcNow,f.PrimarySessionRef!,ProjectFingerprint(project),cp.WorkspaceStamp);
            var next=task with {Goal=anchors.Goal,Acceptance=anchors.Acceptance,Constraints=[anchors.NonGoals,anchors.Boundary]};
            SaveFusion(next,f with {Draft=anchors,Snapshots=f.Snapshots.Add(snapshot)},TaskLifecycle.Ready);
            AuditTask(id,"ReadyFrozen",snapshot.Version.ToString());
        }
    }
    private static void ValidateFrozen(EngineeringTask task,ProjectProfile project)
    {
        var f=task.Fusion!;var snap=f.Frozen??throw new InvalidOperationException("Ready 快照缺失。");
        snap.Anchors.Validate();
        if(snap.PrimarySessionRef!=f.PrimarySessionRef || snap.ProjectFingerprint!=ProjectFingerprint(project) ||
            task.Goal!=snap.Anchors.Goal || !task.Acceptance.SequenceEqual(snap.Anchors.Acceptance) ||
            !task.Constraints.SequenceEqual(new[]{snap.Anchors.NonGoals,snap.Anchors.Boundary}))
            throw new InvalidOperationException("Ready 冻结的目标、验收或项目边界已改变，禁止执行。请恢复配置或新建任务重新讨论。");
    }
    public void ReturnToDiscussion(string id,long revision)
    {
        lock(sync) {
            var task=store.Task(id);var f=task.Fusion??throw new InvalidOperationException();
            if(f.Revision!=revision || task.Lifecycle is not (TaskLifecycle.Active or TaskLifecycle.Ready) || ProjectBusy(task.WorkspaceId))
                throw new InvalidOperationException("只能将未验收且没有执行中的任务返回讨论。");
            SaveFusion(task,f with {DiscussionState="NeedsDiscussion",Error=null},TaskLifecycle.Discussing);
            AuditTask(id,"DiscussionReopened","Owner requested a new Ready revision; prior runs and snapshots retained.");
        }
    }
    public void OwnerAcceptance(string id,string runId,int readyVersion,bool accept,string comment)
    {
        lock(sync) {
            var task=store.Task(id);var f=task.Fusion??throw new InvalidOperationException();
            var run=store.FindRun(runId);
            var latest=store.Runs().Where(r=>r.TaskId==id).OrderByDescending(r=>r.StartedAt).FirstOrDefault();
            if(task.Lifecycle!=TaskLifecycle.AwaitingAcceptance || run is null || run.RunId!=latest?.RunId ||
                run.TaskId!=id || run.State!=RunState.Completed || readyVersion!=f.Frozen?.Version)
                throw new InvalidOperationException("验收请求已失效，请刷新最新交付。");
            if(!accept&&string.IsNullOrWhiteSpace(comment))throw new ArgumentException("Reject 需要说明未满足的验收项。");
            var text=(accept?"Accept: ":"Reject: ")+comment;
            SaveFusion(task,f with {DiscussionState=accept?f.DiscussionState:"NeedsDiscussion",Error=accept?null:"Owner 已拒绝验收。请继续与 Primary 讨论未满足项，再冻结新版本。",Turns=f.Turns.Add(new("Owner",text,DateTimeOffset.UtcNow))},accept?TaskLifecycle.Done:TaskLifecycle.Discussing);
            store.SaveApproval(new(Guid.NewGuid().ToString("N"),runId,accept?"Accepted":"Rejected",accept,text,DateTimeOffset.UtcNow,DateTimeOffset.UtcNow));
            AuditTask(id,accept?"OwnerAccepted":"OwnerRejected",runId);
        }
    }
    private void AuditTask(string id,string kind,string detail)=>store.AppendAsync(new(Guid.NewGuid().ToString("N"),kind,
        DateTimeOffset.UtcNow,id,id,null,null,new Dictionary<string,string>{{"detail",detail}}.ToImmutableDictionary())).GetAwaiter().GetResult();
}
