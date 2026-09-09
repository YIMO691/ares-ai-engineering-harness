using System.Collections.Immutable;
using System.Text.Json;
using Ares.Workbench.Application;
using Ares.Workbench.Adapters.Codex;
using Ares.Workbench.Domain;
using Ares.Workbench.Infrastructure;
namespace Ares.Workbench.Adapters.Codex;

public sealed class RunHandlerFactory(RuntimeSettings settings,IWorkbenchStore events) : IRunHandlerFactory, IDirectEvidence
{
    private sealed class Contexts(ProjectProfile project) : IContextBuilder
    {
        public ValueTask<ContextPackage> BuildAsync(EngineeringTask task,Workspace workspace,WorkflowRun run,CancellationToken ct)
        {
            var instructions=ImmutableDictionary.CreateBuilder<string,string>();
            foreach(var relative in project.InstructionFiles) {
                var path=WorkspacePolicy.Under(workspace.RepoRoot,Path.Combine(workspace.RepoRoot,relative));
                if(new FileInfo(path).Length>100000)throw new InvalidDataException("指令文件超过 100 KB："+relative);
                instructions[relative]=File.ReadAllText(path);
            }
            return ValueTask.FromResult(new ContextPackage(task,workspace,instructions.ToImmutable(),[],run.Executions,task.Constraints,project.InstructionFiles));
        }
    }
    private sealed class Handler(Func<NodeInvocation,CancellationToken,ValueTask<NodeResult>> call) : INodeHandler
    {public ValueTask<NodeResult> ExecuteAsync(NodeInvocation i,CancellationToken ct)=>call(i,ct);}
    public async ValueTask<string> WorkspaceStampAsync(ProjectProfile project,Workspace workspace,CancellationToken ct)
    {
        var env=settings.EnvironmentFor(Path.GetFileName(workspace.ArtifactRoot));
        var values=new List<string>();
        foreach(var args in new[]{new[]{"rev-parse","HEAD"},new[]{"rev-parse","--abbrev-ref","HEAD"}}) {
            var result=await new ProcessRunner([settings.GitExecutable],[workspace.RepoRoot]).RunAsync(
                new(settings.GitExecutable,["-c","safe.directory="+workspace.RepoRoot,..args],workspace.RepoRoot,TimeSpan.FromSeconds(30),env),ct);
            if(!result.Passed)throw new InvalidOperationException("Git prerequisite failed: "+result.Stderr);
            values.Add(result.Stdout.Trim());
        }
        foreach(var relative in project.InstructionFiles) {
            var path=WorkspacePolicy.Under(workspace.RepoRoot,Path.Combine(workspace.RepoRoot,relative));
            values.Add(relative+":"+Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(path))));
        }
        return string.Join("|",values);
    }
    private ProjectCommands Commands(ProjectProfile project,Workspace workspace) {
        var id=Path.GetFileName(workspace.ArtifactRoot);
        return new(project,workspace,settings.GitExecutable,settings.DotnetExecutable,
            LocalPaths.Output(Path.Combine(settings.ScratchRoot,id,"build")),settings.EnvironmentFor(id));
    }
    public async ValueTask<SourceSnapshot> SnapshotAsync(ProjectProfile project,Workspace workspace,CancellationToken ct) =>
        new(await WorkspaceStampAsync(project,workspace,ct),(await Commands(project,workspace).Snapshot(ct)).ToImmutableDictionary(StringComparer.OrdinalIgnoreCase));
    public ValueTask<string> DiffAsync(ProjectProfile project,Workspace workspace,CancellationToken ct) =>
        new(Commands(project,workspace).Diff(ct));
    public bool Writable(ProjectProfile project,Workspace workspace,string path)=>Commands(project,workspace).Writable(path);
    public RunHandlers Create(ProjectProfile project,Workspace workspace)
    {
        string id=Path.GetFileName(workspace.ArtifactRoot);
        var env=settings.EnvironmentFor(id);
        string scratch=LocalPaths.Output(Path.Combine(settings.ScratchRoot,id,"build"));Directory.CreateDirectory(scratch);
        var commands=new ProjectCommands(project,workspace,settings.GitExecutable,settings.DotnetExecutable,scratch,env);
        var codex=new CodexRoleExecutor(new(settings.CodexExecutable,settings.CodexHome,env,[],settings.Model),events);
        string PathFor(string name)=>WorkspacePolicy.Under(workspace.ArtifactRoot,Path.Combine(workspace.ArtifactRoot,name));
        async ValueTask<NodeResult> Role(NodeInvocation i,CodexRole role,CancellationToken ct)
        {
            var before=await commands.Snapshot(ct);
            var writable=role is CodexRole.Engineer or CodexRole.PrimaryImplement;
            var protectedFiles=before.Keys.Where(p=>!writable||!commands.Writable(p)).ToImmutableArray();
            if(writable&&i.Run.Executions.Any(e=>e.NodeId=="prepare"&&e.Result.Outcome==NodeOutcome.Succeeded)) {
                var paths=PathFor("required-write-paths.json");
                if(!File.Exists(paths))return NodeResult.Fail("PROJECT_CONFIGURATION","已完成的准备输出缺失，请 New Run。",false,EffectStatus.Unknown);
                var required=JsonSerializer.Deserialize<string[]>(await File.ReadAllTextAsync(paths,ct))??[];
                var denied=required.Where(p=>!commands.Writable(p)).ToArray();
                if(denied.Length>0)return NodeResult.Fail("PROJECT_POLICY","任务需要写入 "+string.Join(", ",denied)+
                    "，但当前 allowed_paths 未授权或文件受保护。请编辑 Project 添加必要路径，再 Resume 同一 Run。",false,EffectStatus.Unknown);
            }
            static string Compact(string value,int size=12000)=>value.Length<=size?value:value[..(size/2)]+"\n[report excerpt]\n"+value[^(size/2)..];
            var sourceRun=role==CodexRole.PrimaryDiscuss?events.Runs().Where(r=>r.TaskId==i.Task.TaskId).OrderByDescending(r=>r.StartedAt).FirstOrDefault():i.Run;
            var previous=(sourceRun?.Executions??[]).Where(e=>e.NodeId is "prepare" or "review" or "test")
                .GroupBy(e=>e.NodeId).Select(g=>g.Last()).Select(e=>new {e.NodeId,e.Attempt,output=Compact(e.Result.Output),e.Result.Outcome}).ToArray();
            string instructions=role switch {
                CodexRole.PrimaryDiscuss=>"Discuss the Owner message using this native session. Inspect relevant source read-only when needed. Ask focused questions about material uncertainty. Do not implement. Reply in the Owner language. Return ready with ONLY Goal, Acceptance, NonGoals, Boundary, KeyDecisions, Verification and Unresolved; update the current draft to reflect agreed decisions. Keep unresolved questions in Unresolved until actually settled; never infer Owner Ready or acceptance. Internal analysis and tool traces stay native.",
                CodexRole.PrimaryPrepare=>"Understand the task and inspect relevant files. Produce concise grounding (files, symbols, rules, risks, unknowns), plan (steps, tests, acceptance mapping), and exact required_write_paths. Do not implement yet. Keep each report focused; a policy conflict must be reported without failing completed preparation.",
                CodexRole.PrimaryImplement=>"This is PRIMARY_IMPLEMENT / PRIMARY_REWORK. The Owner has authorized implementation of the frozen Ready snapshot when present. Earlier discussion-only/read-only instructions applied to earlier turns; this turn permits edits within CURRENT PROJECT POLICY. Continue this SAME native task, with no separate grounding or planning agent. CURRENT allowed_paths below replace previous project configuration. Address every latest Reviewer finding during rework. Read files before editing and verify saved source. FAST runs prepare and implement within this single invocation. Return changes and limitations.",
                _=>"Independently inspect actual source and provided diff/test evidence against every acceptance criterion. PASS only when requirements are satisfied; otherwise REWORK_REQUIRED with actionable severity, requirement, issue, evidence and requested_fix. Do not invent findings, implement code or redo full grounding."
            };
            string diff=role==CodexRole.Reviewer?await commands.Diff(ct):"";
            string Section(string name)=>File.Exists(PathFor(name+".md"))?Compact(File.ReadAllText(PathFor(name+".md")),6000):"";
            var cp=events.Checkpoint(i.Run.RunId);
            var session=role==CodexRole.Reviewer?cp?.ReviewerSessionRef:cp?.PrimarySessionRef;
            var prompt=instructions+"\nCURRENT TASK AND PROJECT POLICY:\n"+JsonSerializer.Serialize(new {
                task=i.Task with {Fusion=null,Direct=null},ready=i.Task.Fusion?.Frozen,
                direct_agreement=i.Task.Direct?.Agreement is {} da?new {da.Version,da.Anchors,da.Checks}:null,
                external_primary_report=i.Task.Direct?.LastReport,
                last_owner_rejection=role==CodexRole.PrimaryDiscuss?i.Task.Fusion?.Turns.LastOrDefault(t=>t.Speaker=="Owner"&&t.Text.StartsWith("Reject:"))?.Text:null,
                current_draft=role==CodexRole.PrimaryDiscuss?i.Task.Fusion?.Draft:null,
                owner_message=role==CodexRole.PrimaryDiscuss?i.Task.Fusion?.Turns.LastOrDefault(t=>t.Speaker=="Owner")?.Text:null,
                project,workspace,grounding=Section("grounding"),plan=Section("plan"),latest_business_results=previous,
                instruction_files=i.Context.SelectedFiles,rework_count=i.Run.ReworkCount,git_diff=diff,
                continuation=new {run_id=i.Run.RunId,resume_target=i.Node.NodeId,completed_steps=cp?.CompletedSteps}
            },SqliteWorkbenchStore.Json);
            if(i.Task.Fusion is not null && role==CodexRole.PrimaryImplement && session!=i.Task.Fusion.PrimarySessionRef)
                return NodeResult.Fail("CODEX_SESSION_MISMATCH","Primary 原生会话绑定缺失或改变，禁止启动替代会话。",false,EffectStatus.Unknown);
            if(role==CodexRole.Reviewer && session is not null && session==cp?.PrimarySessionRef)
                return NodeResult.Fail("CODEX_SESSION_MISMATCH","Reviewer 必须使用独立会话。",false,EffectStatus.Unknown);
            var result=await codex.ExecuteAsync(new(role,i,prompt,protectedFiles,session),ct);
            var after=await commands.Snapshot(CancellationToken.None);
            var changed=before.Keys.Union(after.Keys,StringComparer.OrdinalIgnoreCase)
                .Where(p=>before.GetValueOrDefault(p)!=after.GetValueOrDefault(p)).ToArray();
            if(changed.Any(p=>!writable||!commands.Writable(p)))
                return NodeResult.Fail("ROLE_BOUNDARY_VIOLATION","角色改动超出授权路径："+string.Join(", ",changed),false,EffectStatus.Unknown) with {ArtifactRefs=result.ArtifactRefs};
            if(writable && !ct.IsCancellationRequested) {
                var path=PathFor("diff-r"+i.Run.ReworkCount+"-a"+(i.Run.Executions.Count(e=>e.NodeId==i.Node.NodeId)+1)+".patch");
                await File.WriteAllTextAsync(path,await commands.Diff(ct),ct);
                var changedPath=PathFor("changed-files-r"+i.Run.ReworkCount+"-a"+(i.Run.Executions.Count(e=>e.NodeId==i.Node.NodeId)+1)+".json");
                await File.WriteAllTextAsync(changedPath,JsonSerializer.Serialize(changed),ct);
                result=result with {ArtifactRefs=result.ArtifactRefs.Add(path).Add(changedPath)};
            }
            return result;
        }
        async ValueTask<NodeResult> Test(NodeInvocation i,CancellationToken ct)
        {
            var artifacts=new List<string>();var output=new System.Text.StringBuilder();
            foreach(var (kind,command) in new[]{("build",project.BuildCommand),("test",project.TestCommand)}) {
                var p=await commands.Run(command,ct);
                var text=PathFor(kind+"-r"+i.Run.ReworkCount+"-a"+(i.Run.Executions.Count(e=>e.NodeId==i.Node.NodeId)+1)+".txt");
                var json=PathFor(kind+"-r"+i.Run.ReworkCount+"-a"+(i.Run.Executions.Count(e=>e.NodeId==i.Node.NodeId)+1)+".json");
                await File.WriteAllTextAsync(text,command+"\n"+p.Stdout+"\n"+p.Stderr,CancellationToken.None);
                await File.WriteAllTextAsync(json,JsonSerializer.Serialize(p,SqliteWorkbenchStore.Json),CancellationToken.None);
                artifacts.Add(text);artifacts.Add(json);
                output.AppendLine(kind+": "+(p.Passed?"PASS":"FAIL")).AppendLine(p.Stdout).AppendLine(p.Stderr);
                if(p.Cancelled)return new(NodeOutcome.Cancelled,"node-result/v1","Verification interrupted.",[..artifacts]);
                if(!p.Passed && System.Text.RegularExpressions.Regex.IsMatch(p.Stdout+"\n"+p.Stderr,@"\b(NU1301|NU1900)\b"))
                    return NodeResult.Fail("EXECUTOR_ENVIRONMENT","Dependency service or vulnerability-feed access failed; repair connectivity and retry verification.") with {ArtifactRefs=[..artifacts]};
                if(!p.Passed && (i.Task.Fusion is not null || i.Task.Direct is not null) && p.ExitCode!=0 && !p.TimedOut && p.Failure is null)
                    return NodeResult.Rework("Verification failed. Fix the actual diagnostics without weakening acceptance:\n"+output,[..artifacts]);
                if(!p.Passed)return NodeResult.Fail("VERIFICATION_FAILED",kind+" 命令失败；请查看测试输出。",false,EffectStatus.Unknown) with {ArtifactRefs=[..artifacts]};
            }
            return NodeResult.Success(output.ToString(),[..artifacts]);
        }
        async ValueTask<NodeResult> Deliver(NodeInvocation i,CancellationToken ct)
        {
            var diff=PathFor("final.diff.patch");await File.WriteAllTextAsync(diff,await commands.Diff(ct),ct);
            var summary=PathFor("delivery.md");
            if(i.Task.Fusion?.Frozen is {} ready) {
                var report=new System.Text.StringBuilder("# "+i.Task.Title+"\n\nAwaiting Owner acceptance · Ready v"+ready.Version+"\n");
                report.AppendLine("\n## Acceptance checklist");
                foreach(var criterion in ready.Anchors.Acceptance)report.AppendLine("- [ ] Owner confirms: "+criterion);
                report.AppendLine("\n## Verification / Review / Changes");
                foreach(var step in i.Run.Executions.Where(e=>e.NodeId is "codex" or "test" or "review").GroupBy(e=>e.NodeId).Select(g=>g.Last()))
                    report.AppendLine("\n### "+step.NodeId+" · "+step.Result.Outcome+"\n"+step.Result.Output);
                report.AppendLine("\n## Remaining risk\nPassing automated checks does not replace Owner acceptance. Review the actual diff and any limitations in the Primary and Reviewer reports above.");
                report.AppendLine("\nRework count: "+i.Run.ReworkCount+"\nNo commit or push was performed.");
                await File.WriteAllTextAsync(summary,report.ToString(),ct);
                return NodeResult.Success(report.ToString(),diff,summary);
            }
            await File.WriteAllTextAsync(summary,"# "+i.Task.Title+"\n\n所有必需节点通过。\n\nRun: "+i.Run.RunId+"\n返工次数: "+i.Run.ReworkCount+"\n未自动提交或推送。",ct);
            return NodeResult.Success("工作流验证完成，修改保留在本地项目中。",diff,summary);
        }
        return new(new Contexts(project),new Dictionary<string,INodeHandler> {
            ["discuss"]=new Handler((i,ct)=>Role(i,CodexRole.PrimaryDiscuss,ct)),
            ["prepare"]=new Handler((i,ct)=>Role(i,CodexRole.PrimaryPrepare,ct)),
            ["codex"]=new Handler((i,ct)=>Role(i,CodexRole.PrimaryImplement,ct)),
            ["review"]=new Handler((i,ct)=>Role(i,CodexRole.Reviewer,ct)),
            ["test"]=new Handler(Test),["deliver"]=new Handler(Deliver),["human"]=new HumanNodeHandler()
        });
    }
}
