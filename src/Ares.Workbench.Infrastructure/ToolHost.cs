using System.Collections.Immutable;
using System.Text.Json;
using Ares.Workbench.Application;
using Ares.Workbench.Domain;
namespace Ares.Workbench.Infrastructure;

public sealed record ToolConfiguration(string Dotnet,string Git,string BuildArtifacts,ImmutableDictionary<string,string> Environment);
public sealed class ToolHost(Workspace workspace,ToolConfiguration configuration,IEventSink events)
{
    private readonly ProcessRunner processes=new([configuration.Dotnet,configuration.Git],[workspace.RepoRoot]);
    private readonly WorkspacePolicy policy=new(workspace);
    public static ImmutableArray<ToolDefinition> Definitions=>[
        new("FileRead","relative-path/v1","text/v1",SideEffect.ReadOnly,TimeSpan.FromSeconds(5),"workspace.read"),
        new("CodeSearch","text-query/v1","paths/v1",SideEffect.ReadOnly,TimeSpan.FromSeconds(5),"workspace.read"),
        new("GitDiff","empty/v1","diff/v1",SideEffect.ReadOnly,TimeSpan.FromSeconds(20),"workspace.read"),
        new("Build","project-path/v1","process-result/v1",SideEffect.WorkspaceWrite,TimeSpan.FromMinutes(2),"workspace.build"),
        new("Test","test-command/v1","process-result/v1",SideEffect.ReadOnly,TimeSpan.FromMinutes(2),"workspace.test")];
    private async ValueTask Emit(NodeInvocation i,string type,string tool,string status) =>
        await events.AppendAsync(new(Guid.NewGuid().ToString("N"),type,DateTimeOffset.UtcNow,i.Task.TaskId,i.Run.RunId,
            i.Node.NodeId,i.Run.Executions.Count(x=>x.NodeId==i.Node.NodeId)+1,
            new Dictionary<string,string>{{"tool",tool},{"status",status}}.ToImmutableDictionary()));
    public async ValueTask<string> Read(NodeInvocation i,string relative)
    {
        await Emit(i,"ToolCalled","FileRead","STARTED");
        var text=policy.Read(relative);await Emit(i,"ToolFinished","FileRead","SUCCESS");return text;
    }
    public async ValueTask<IReadOnlyList<string>> Search(NodeInvocation i,string query,IEnumerable<string> paths)
    {
        await Emit(i,"ToolCalled","CodeSearch","STARTED");
        var matches=policy.Search(query,paths);await Emit(i,"ToolFinished","CodeSearch","SUCCESS");return matches;
    }
    public async ValueTask<NodeResult> Diff(NodeInvocation i,CancellationToken ct)
    {
        await Emit(i,"ToolCalled","GitDiff","STARTED");
        var p=await processes.RunAsync(new(configuration.Git,["diff","--no-ext-diff","--no-textconv","HEAD","--","."],workspace.RepoRoot,
            TimeSpan.FromSeconds(20),configuration.Environment),ct);
        var path=Artifact($"diff.r{i.Run.ReworkCount}.patch");
        await File.WriteAllTextAsync(path,p.Stdout,ct);
        await Emit(i,"ToolFinished","GitDiff",p.Passed?"SUCCESS":"FAILED");
        return p.Passed?NodeResult.Success("Git diff captured.",path):NodeResult.Fail("GIT_DIFF_FAILED",p.Stderr);
    }
    public async ValueTask<NodeResult> Build(NodeInvocation i,string project,CancellationToken ct)
    {
        policy.Resolve(project);
        await Emit(i,"ToolCalled","Build","STARTED");
        var p=await processes.RunAsync(new(configuration.Dotnet,["build",project,"-p:AresArtifactsRoot="+configuration.BuildArtifacts,"--verbosity","minimal"],
            workspace.RepoRoot,TimeSpan.FromMinutes(2),configuration.Environment),ct);
        var output=Artifact($"build.r{i.Run.ReworkCount}.txt");
        await File.WriteAllTextAsync(output,p.Stdout+"\\n"+p.Stderr,ct);
        await Emit(i,"ToolFinished","Build",p.Passed?"SUCCESS":"FAILED");
        return p.Passed?NodeResult.Success("Actual dotnet build passed.",output):NodeResult.Fail("BUILD_FAILED","See build output.") with {ArtifactRefs=[output]};
    }
    public async ValueTask<NodeResult> Test(NodeInvocation i,string testProject,CancellationToken ct,string? filter=null)
    {
        policy.Resolve(testProject);
        await Emit(i,"ToolCalled","Test","STARTED");
        var args=new List<string>{"test",testProject,"-p:AresArtifactsRoot="+configuration.BuildArtifacts,"--verbosity","minimal"};
        if(filter is not null){args.Add("--filter");args.Add(filter);}
        var p=await processes.RunAsync(new(configuration.Dotnet,[..args],
            workspace.RepoRoot,TimeSpan.FromMinutes(2),configuration.Environment),ct);
        var output=Artifact($"{i.Node.NodeId}.r{i.Run.ReworkCount}.txt");
        await File.WriteAllTextAsync(output,p.Stdout+"\n"+p.Stderr,ct);
        var result=Artifact($"{i.Node.NodeId}.r{i.Run.ReworkCount}.json");
        await File.WriteAllTextAsync(result,JsonSerializer.Serialize(p),ct);
        await Emit(i,"ToolFinished","Test",p.Passed?"SUCCESS":"FAILED");
        if(p.Passed)return NodeResult.Success("Actual dotnet test passed.",output,result);
        if(p.Cancelled)return new(NodeOutcome.Cancelled,"node-result/v1","Test cancelled.",[output,result]);
        if(p.TimedOut)return NodeResult.Fail("TEST_TIMEOUT","dotnet test timed out.");
        return i.Task.Risk!=Risk.Fast?NodeResult.Rework("Actual dotnet test failed; inspect test output.",output,result):
            NodeResult.Fail("TEST_FAILED",p.Stderr);
    }
    public string Artifact(string filename)
    {
        var root=WorkspacePolicy.ValidateRoot(workspace.ArtifactRoot);
        var path=WorkspacePolicy.Under(root,Path.Combine(root,filename));Directory.CreateDirectory(root);return path;
    }
}
public sealed class FileContextBuilder(ImmutableArray<string> selectedPaths) : IContextBuilder
{
    public ValueTask<ContextPackage> BuildAsync(EngineeringTask task,Workspace workspace,WorkflowRun run,CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var policy=new WorkspacePolicy(workspace);
        var files=selectedPaths.ToImmutableDictionary(p=>p,p=>policy.Read(p));
        return ValueTask.FromResult(new ContextPackage(task,workspace,files,[],run.Executions,task.Constraints,
            [..selectedPaths.Select(p=>p+" @ "+workspace.GitRef)]));
    }
}
public sealed class DelegateNodeHandler(Func<NodeInvocation,CancellationToken,ValueTask<NodeResult>> execute) : INodeHandler
{
    public ValueTask<NodeResult> ExecuteAsync(NodeInvocation invocation,CancellationToken cancellationToken)=>execute(invocation,cancellationToken);
}
