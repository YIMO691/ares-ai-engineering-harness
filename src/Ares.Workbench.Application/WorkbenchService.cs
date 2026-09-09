using System.Collections.Concurrent;
using System.Collections.Immutable;
using Ares.Workbench.Domain;
namespace Ares.Workbench.Application;

/// <summary>Product mutations and business checkpoints; execution remains in WorkflowCoordinator and Codex.</summary>
public sealed partial class WorkbenchService(IWorkbenchStore store,IProjectWorkspace workspaces,
    IRunHandlerFactory handlers,IWorkflowBackend backend,IWorkbenchQueue queue)
{
    private readonly object sync=new();
    private readonly ConcurrentDictionary<string,WorkflowCoordinator> waiting=new();
    private readonly HashSet<string> pendingDecisions=[];
    private sealed class RunningControl(CancellationTokenSource cancellation) {
        public CancellationTokenSource Cancellation {get;}=cancellation;
        public volatile bool Pause;
        public volatile bool Requested;
    }
    private readonly Dictionary<string,RunningControl> executing=[];
    public IWorkbenchStore Read=>store;
    private static bool Active(WorkflowRun r)=>r.State is RunState.Created or RunState.Running or RunState.Waiting;
    private bool ProjectBusy(string id)=>store.Tasks().Any(t=>t.WorkspaceId==id&&t.Fusion?.DiscussionState is "Queued" or "Running")
        ||store.Tickets().Any(t=>t.Project.ProjectId==id && t.State is "Queued" or "Running")
        ||store.Runs().Any(r=>Active(r)&&store.Task(r.TaskId).WorkspaceId==id);
    public ProjectProfile SaveProject(ProjectProfile profile)
    {
        lock(sync) {
            if(ProjectBusy(profile.ProjectId))throw new InvalidOperationException("项目正在运行或等待审批，结束后再修改配置。");
            if(string.IsNullOrWhiteSpace(profile.Name)||!Enum.IsDefined(profile.DefaultRisk))throw new ArgumentException("请填写项目名称和风险等级。");
            var valid=workspaces.Validate(profile with {Name=profile.Name.Trim(),UpdatedAt=DateTimeOffset.UtcNow});
            store.SaveProject(valid);return valid;
        }
    }
    public EngineeringTask CreateTask(string projectId,string title,string goal,IEnumerable<string> acceptance,IEnumerable<string> constraints,Risk risk)
    {
        var project=store.Project(projectId);
        var required=acceptance.Where(x=>!string.IsNullOrWhiteSpace(x)).Select(x=>x.Trim()).ToImmutableArray();
        if(string.IsNullOrWhiteSpace(title)||string.IsNullOrWhiteSpace(goal)||required.IsEmpty||!Enum.IsDefined(risk))
            throw new ArgumentException("请填写标题、目标、至少一条验收标准和风险等级。");
        var now=DateTimeOffset.UtcNow;
        var task=new EngineeringTask(Guid.NewGuid().ToString("N"),title.Trim(),goal.Trim(),required,
            constraints.Where(x=>!string.IsNullOrWhiteSpace(x)).ToImmutableArray(),risk,project.ProjectId,TaskLifecycle.Draft,now,now);
        ((ITaskStore)store).Add(task);return task;
    }
    public string RequestRun(string taskId)
    {
        lock(sync) {
            var task=store.Task(taskId);var project=workspaces.Validate(store.Project(task.WorkspaceId));
            if(task.Lifecycle is TaskLifecycle.Delivered or TaskLifecycle.Cancelled or TaskLifecycle.Done)throw new InvalidOperationException("该任务已结束，请新建任务。");
            if(task.Fusion is {} fusion) {
                if(task.Lifecycle!=TaskLifecycle.Ready || fusion.Frozen is null)throw new InvalidOperationException("请先完成讨论并点击 Ready。");
                ValidateFrozen(task,project);
            }
            if(ProjectBusy(project.ProjectId))throw new InvalidOperationException("项目已有排队、运行或等待审批的任务。");
            var ticket=new RunTicket(Guid.NewGuid().ToString("N"),taskId,project,"Queued",DateTimeOffset.UtcNow);
            store.SaveTicket(ticket);queue.Enqueue(new(ticket.RunId));return ticket.RunId;
        }
    }
    public bool CanResume(string id) {
        var run=store.FindRun(id);var cp=store.Checkpoint(id);
        if(run is not null && store.Task(run.TaskId) is {Fusion:not null,Lifecycle:not TaskLifecycle.Active})return false;
        return run?.State is RunState.Paused or RunState.Blocked && cp is {ResumeSupported:true,ResumeTarget:not null}
            && store.Ticket(id).State is not ("Queued" or "Running")
            && run.Failure?.Code is not ("REWORK_BUDGET" or "HUMAN_REJECTED" or "INTERRUPTED" or "RESUME_INVALID");
    }
    public void RequestResume(string id)
    {
        lock(sync) {
            if(!CanResume(id))throw new InvalidOperationException("该 Run 不可继续；请新建 Run。旧版和重启中断记录仅保留历史。");
            var ticket=store.Ticket(id);var task=store.Task(ticket.TaskId);var cp=store.Checkpoint(id)!;
            var project=workspaces.Validate(store.Project(task.WorkspaceId));
            if(ProjectBusy(project.ProjectId))throw new InvalidOperationException("项目当前已有执行或审批。");
            if(task.Lifecycle is TaskLifecycle.Delivered or TaskLifecycle.Cancelled or TaskLifecycle.Done ||
                cp.TaskFingerprint!=RunCheckpoint.Fingerprint(task) ||
                !string.Equals(cp.RepoRoot,project.RepoRoot,StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Task 或仓库根目录已改变，必须 New Run。");
            if(task.Fusion is not null)ValidateFrozen(task,project);
            var run=store.FindRun(id)!;
            if((ticket.Project.BuildCommand!=project.BuildCommand||ticket.Project.TestCommand!=project.TestCommand)
                && run.CurrentNode is "review" or "human" or "deliver"
                && run.Executions.LastOrDefault(e=>e.NodeId=="test")?.Result.Outcome==NodeOutcome.Succeeded) {
                // Preserve preparation; verification must use the current commands.
                ((IRunStore)store).Update(run with {CurrentNode="test",Version=run.Version+1,UpdatedAt=DateTimeOffset.UtcNow});
            }
            store.SaveTicket(ticket with {Project=project,State="Queued",Error=null});
            queue.Enqueue(new(id,Resume:true));
        }
    }
    public void RequestStop(string id,bool cancel)
    {
        lock(sync) {
            var run=store.FindRun(id);var ticket=store.Ticket(id);
            if(executing.TryGetValue(id,out var control) && (run is null || run.State==RunState.Running)) {
                if(control.Requested)throw new InvalidOperationException("中断请求正在处理，请等待进程退出。");
                control.Pause=!cancel;control.Requested=true;control.Cancellation.Cancel();return;
            }
            if(!cancel)throw new InvalidOperationException("当前没有可中断的执行步骤。");
            if(pendingDecisions.Contains(id))throw new InvalidOperationException("审批正在处理，请稍后刷新。");
            if(run is not null && run.State is not (RunState.Paused or RunState.Blocked or RunState.Waiting)
                ||run is null&&ticket.State!="Queued")
                throw new InvalidOperationException("Run 已结束，无法取消。");
            // A queued resume is invalidated here; the queue reader ignores cancelled tickets.
            store.SaveTicket(ticket with {State="Cancelled"});
            if(run is not null) {
                ((IRunStore)store).Update(run with {State=RunState.Cancelled,PendingHuman=null,
                    Failure=new("CANCELLED","Owner 已取消 Run；已有源码改动保留。"),Version=run.Version+1,UpdatedAt=DateTimeOffset.UtcNow});
                waiting.TryRemove(id,out _);
                foreach(var approval in store.Approvals(id).Where(a=>a.Status=="Waiting"))
                    store.SaveApproval(approval with {Status="Cancelled",DecidedAt=DateTimeOffset.UtcNow});
            }
            store.AppendAsync(new(Guid.NewGuid().ToString("N"),"WorkflowCancelled",DateTimeOffset.UtcNow,ticket.TaskId,id,
                run?.CurrentNode,null,ImmutableDictionary<string,string>.Empty)).GetAwaiter().GetResult();
        }
    }
    public void RequestApproval(string runId,string requestId,long version,bool approve,string comment)
    {
        lock(sync) {
            var run=store.FindRun(runId)??throw new KeyNotFoundException();var pending=run.PendingHuman;
            if(run.State!=RunState.Waiting||pending is null||pending.RequestId!=requestId||pending.RunVersion!=version||!waiting.ContainsKey(runId))
                throw new InvalidOperationException("审批已失效或应用已重启，请刷新页面。");
            if(!pendingDecisions.Add(runId))throw new InvalidOperationException("审批决定正在处理。");
            queue.Enqueue(new(runId,new(requestId,pending.Scope,version,"LocalOwner",approve,string.IsNullOrWhiteSpace(comment)?(approve?"Owner approved":"Owner rejected"):comment)));
        }
    }
    public async Task ProcessAsync(QueueItem item,CancellationToken ct)
    {
        if(item.Discuss){await ProcessDiscussionAsync(item.RunId,ct);return;}
        if(item.Decision is not null) {
            try {
                if(!waiting.TryGetValue(item.RunId,out var coordinator))throw new InvalidOperationException("Waiting execution unavailable.");
                var run=await coordinator.SubmitHumanAsync(item.RunId,item.Decision,ct);
                if(!item.Decision.Approved&&run.State==RunState.Failed)
                    ((IRunStore)store).Update(run with {State=RunState.Blocked,Version=run.Version+1,UpdatedAt=DateTimeOffset.UtcNow});
                var old=store.Approvals(item.RunId).Last(a=>a.Status=="Waiting");
                store.SaveApproval(old with {Status=item.Decision.Approved?"Approved":"Rejected",Decision=item.Decision.Approved,Comment=item.Decision.Reason,DecidedAt=DateTimeOffset.UtcNow});
                waiting.TryRemove(item.RunId,out _);
            } finally {lock(sync)pendingDecisions.Remove(item.RunId);}
            return;
        }
        RunTicket ticket;RunningControl control;
        using var linked=CancellationTokenSource.CreateLinkedTokenSource(ct);
        lock(sync) {
            ticket=store.Ticket(item.RunId);
            if(ticket.State!="Queued")return;
            control=new(linked);executing.Add(item.RunId,control);
            store.SaveTicket(ticket with {State="Running"});
        }
        try {
            var project=workspaces.Validate(store.Project(ticket.Project.ProjectId));
            var workspace=workspaces.Create(project,item.RunId);
            var task=store.Task(ticket.TaskId);
            var stamp=await handlers.WorkspaceStampAsync(project,workspace,linked.Token);
            if(task.Fusion is {} fusion) {
                ValidateFrozen(task,project);
                if(fusion.Frozen!.WorkspaceStamp!=stamp)throw new ResumeInvalidException();
            }
            if(item.Resume) {
                var cp=store.Checkpoint(item.RunId)??throw new InvalidOperationException("Checkpoint unavailable.");
                if(cp.TaskFingerprint!=RunCheckpoint.Fingerprint(task)||!string.Equals(cp.RepoRoot,project.RepoRoot,StringComparison.OrdinalIgnoreCase)||(cp.WorkspaceStamp!=stamp && !(cp.WorkspaceStamp.Length==0 && store.FindRun(item.RunId)!.Executions.IsEmpty)))
                    throw new ResumeInvalidException();
                if(cp.WorkspaceStamp.Length==0)store.UpdateCheckpoint(item.RunId,c=>c with {WorkspaceStamp=stamp});
            } else {
                store.SaveCheckpoint(new RunCheckpoint(item.RunId,RunState.Created,null,null,null,null,[],0,null,null,DateTimeOffset.UtcNow,
                    RunCheckpoint.Fingerprint(task),project.RepoRoot,stamp) with {PrimarySessionRef=task.Fusion?.PrimarySessionRef});
            }
            var parts=handlers.Create(project,workspace);
            var coordinator=new WorkflowCoordinator(store,store,store,backend,parts.Contexts,parts.Handlers,()=>control.Pause,sync);
            var definition=Definition(task);
            var run=item.Resume?await coordinator.ResumeAsync(item.RunId,definition,workspace,linked.Token):
                await coordinator.StartAsync(ticket.TaskId,definition,workspace,linked.Token,item.RunId);
            if(run.State==RunState.Waiting) {
                waiting[item.RunId]=coordinator;
                store.SaveApproval(new(run.PendingHuman!.RequestId,run.RunId,"Waiting",null,run.PendingHuman.Reason,DateTimeOffset.UtcNow,null));
            }
            store.SaveTicket(ticket with {Project=project,State=run.State==RunState.Cancelled?"Cancelled":"Finished"});
        } catch(Exception ex) {
            var current=store.FindRun(item.RunId);
            var cancelled=ex is OperationCanceledException;
            var code=ex is ResumeInvalidException?"RESUME_INVALID":cancelled?"CANCELLED":"PROJECT_CONFIGURATION";
            var message=ex is ResumeInvalidException?"Task、仓库、Git ref 或指令文件已改变，必须 New Run。":ex.Message;
            store.SaveTicket(ticket with {State=cancelled&&!control.Pause?"Cancelled":"Finished",Error=message});
            if(current is null) {
                // A prerequisite failure is a visible business Run, so the Owner can repair and resume it.
                var task=store.Task(ticket.TaskId);var definition=Definition(task);var now=DateTimeOffset.UtcNow;
                var state=cancelled?(control.Pause?RunState.Paused:RunState.Cancelled):RunState.Blocked;
                current=new(item.RunId,ticket.TaskId,definition.WorkflowId,definition.Version,backend.BackendId,state,
                    definition.EntryNode,0,now,now,0,[],null,new(cancelled&&control.Pause?"STOPPED":code,message));
                ((IRunStore)store).Add(current);
                ((ITaskStore)store).Update(task with {Lifecycle=TaskLifecycle.Active,UpdatedAt=now});
                store.SaveCheckpoint((new RunCheckpoint(item.RunId,state,definition.EntryNode,definition.EntryNode,message,
                    RunCheckpoint.Category(code),[],0,null,null,now,RunCheckpoint.Fingerprint(task),ticket.Project.RepoRoot,"") with {PrimarySessionRef=task.Fusion?.PrimarySessionRef}).WithRun(current));
            }
            if(current is not null && current.State!=RunState.Completed) {
                ((IRunStore)store).Update(current with {State=cancelled?(control.Pause?RunState.Paused:RunState.Cancelled):RunState.Blocked,
                    Failure=new(code,message),Version=current.Version+1,UpdatedAt=DateTimeOffset.UtcNow});
                if(ex is ResumeInvalidException)store.UpdateCheckpoint(item.RunId,c=>c with {ResumeSupported=false});
            }
            await store.AppendAsync(new(Guid.NewGuid().ToString("N"),"RunHostFailed",DateTimeOffset.UtcNow,ticket.TaskId,item.RunId,null,null,
                new Dictionary<string,string>{{"message",message}}.ToImmutableDictionary()),CancellationToken.None);
        } finally {lock(sync)executing.Remove(item.RunId);}
    }
    private static WorkflowDefinition Definition(EngineeringTask task)=>task.Fusion is null?MvpWorkflowCatalog.CreateV02(task.Risk):MvpWorkflowCatalog.CreateFusion(task.Risk);
    private sealed class ResumeInvalidException:Exception;
    public async Task RecoverInterruptedAsync()
    {
        foreach(var task in store.Tasks().Where(t=>t.Fusion?.DiscussionState is "Queued" or "Running")) {
            var fusion=task.Fusion!;
            SaveFusion(task,fusion with {DiscussionState="Blocked",Error="应用重启中断讨论；发送下一条消息续接已记录的原生 session。",PrimarySessionRef=store.Checkpoint(task.TaskId)?.PrimarySessionRef??fusion.PrimarySessionRef});
        }
        foreach(var run in store.Runs().Where(r=>Active(r)||r.State==RunState.Paused)) {
            ((IRunStore)store).Update(run with {State=RunState.Blocked,PendingHuman=null,Failure=new("INTERRUPTED","应用已重启；本版本不恢复中断执行，请 New Run。"),UpdatedAt=DateTimeOffset.UtcNow,Version=run.Version+1});
            foreach(var approval in store.Approvals(run.RunId).Where(a=>a.Status=="Waiting"))
                store.SaveApproval(approval with {Status="Interrupted",DecidedAt=DateTimeOffset.UtcNow});
            await store.AppendAsync(new(Guid.NewGuid().ToString("N"),"RunInterruptedByRestart",DateTimeOffset.UtcNow,run.TaskId,run.RunId,null,null,ImmutableDictionary<string,string>.Empty));
        }
        // Restart recovery is outside v0.2: do not imply in-memory resumability survives a crash.
        foreach(var run in store.Runs().Where(r=>store.Checkpoint(r.RunId) is not null))
            store.UpdateCheckpoint(run.RunId,c=>c with {ResumeSupported=false});
        foreach(var ticket in store.Tickets().Where(t=>t.State is "Queued" or "Running")) {
            store.SaveTicket(ticket with {State="Interrupted",Error="应用重启，队列未恢复。"});
            if(store.FindRun(ticket.RunId) is null)
                await store.AppendAsync(new(Guid.NewGuid().ToString("N"),"RunInterruptedByRestart",DateTimeOffset.UtcNow,ticket.TaskId,ticket.RunId,null,null,ImmutableDictionary<string,string>.Empty));
        }
    }
}
