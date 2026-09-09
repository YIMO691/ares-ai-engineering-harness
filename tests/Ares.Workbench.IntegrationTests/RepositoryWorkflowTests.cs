using System.Collections.Immutable;
using System.Text.Json;
using Ares.Workbench.Application;
using Ares.Workbench.Domain;
using Ares.Workbench.Infrastructure;
using Ares.Workbench.Adapters.AgentFramework;
namespace Ares.Workbench.IntegrationTests;
public class RepositoryWorkflowTests
{
    private static string Env(string key)=>Environment.GetEnvironmentVariable(key)??throw new InvalidOperationException(key+" required");
    [Fact]public async Task NonTodoRepositoryUsesRealTestsAndGitDiffWithSameRunRework()
    {
        var parent=Path.Combine(Path.GetTempPath(),"ares-integration",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(parent);
        var repo=Path.Combine(parent,"repo");var artifacts=WorkspacePolicy.Under(WorkspacePolicy.ValidateRoot(Env("ARES_EVIDENCE")),Path.Combine(Env("ARES_EVIDENCE"),"scripted-standard",Guid.NewGuid().ToString("N")));
        var env=new Dictionary<string,string>{{"DOTNET_ROOT",Path.GetDirectoryName(Env("ARES_DOTNET"))!}}.ToImmutableDictionary();
        var process=new ProcessRunner([Env("ARES_GIT")],[parent]);
        var clone=await process.RunAsync(new(Env("ARES_GIT"),["clone","--local","--no-hardlinks",Env("ARES_DEMO_SOURCE"),repo],parent,TimeSpan.FromSeconds(20),env));
        Assert.True(clone.Passed,clone.Stderr);
        var workspace=new Workspace("different-workspace",repo,"HEAD",["."],artifacts,"local");
        var events=new JsonlEventSink(artifacts);
        var tools=new ToolHost(workspace,new(Env("ARES_DOTNET"),Env("ARES_GIT"),Path.Combine(parent,"build"),env),events);
        var taskStore=new InMemoryTaskStore();var runStore=new InMemoryRunStore();
        new TaskService(taskStore).Create("arbitrary-task-id","Labels","Normalize labels",["trim","reject whitespace"],[],Risk.Standard,workspace);
        var calls=0;
        var handlers=new Dictionary<string,INodeHandler>{
            ["grounding"]=new DelegateNodeHandler(async(i,ct)=>{
                Assert.Contains("LabelFormatter",await tools.Read(i,"src/LabelFormatter.cs"));
                Assert.Single(await tools.Search(i,"Normalize",["src/LabelFormatter.cs"]));
                return await tools.Build(i,"src/LabelFormatter.csproj",ct);
            }),
            ["plan"]=new DelegateNodeHandler((i,ct)=>ValueTask.FromResult(NodeResult.Success("Scenario fixture plan."))),
            ["codex"]=new DelegateNodeHandler(async(i,ct)=>{
                calls++;
                var body=i.Run.ReworkCount==0?"return input.Trim();":"if(string.IsNullOrWhiteSpace(input))throw new ArgumentException();return input.Trim();";
                await File.WriteAllTextAsync(new WorkspacePolicy(workspace).Resolve("src/LabelFormatter.cs"),
                    "namespace LabelDemo; public static class LabelFormatter { public static string Normalize(string input) { ArgumentNullException.ThrowIfNull(input);"+body+"} }",ct);
                return NodeResult.Success("Scripted executor fixture; not real Codex.");
            }),
            ["test"]=new DelegateNodeHandler((i,ct)=>tools.Test(i,"tests/LabelFormatter.Tests.csproj",ct,"Category!=Acceptance")),
            ["review"]=new DelegateNodeHandler((i,ct)=>tools.Test(i,"tests/LabelFormatter.Tests.csproj",ct,"Category=Acceptance")),
            ["deliver"]=new DelegateNodeHandler((i,ct)=>tools.Diff(i,ct))
        };
        var runtime=new WorkflowCoordinator(taskStore,runStore,events,new MicrosoftAgentFrameworkBackend(),
            new FileContextBuilder(["src/LabelFormatter.cs"]),handlers);
        var run=await runtime.StartAsync("arbitrary-task-id",WorkflowCatalog.Create(Risk.Standard),workspace);
        await File.WriteAllTextAsync(Path.Combine(artifacts,"run-summary.json"),JsonSerializer.Serialize(new {run,real_codex_run="NOT_VERIFIED",executor="CSharp scripted test fixture"}));
        await File.WriteAllTextAsync(Path.Combine(Directory.GetParent(artifacts)!.FullName,"latest.txt"),artifacts);
        Assert.Equal(RunState.Completed,run.State);Assert.Equal(1,run.ReworkCount);Assert.Equal(2,calls);
        Assert.Equal(TaskLifecycle.Delivered,runtime.Task(run.TaskId).Lifecycle);
        Assert.Contains("IsNullOrWhiteSpace",await File.ReadAllTextAsync(Path.Combine(artifacts,"diff.r1.patch")));
        var lines=await File.ReadAllLinesAsync(Path.Combine(artifacts,"events.jsonl"));
        Assert.Contains(lines,line=>line.Contains("ReworkRequested"));
        Assert.All(lines,line=>{using var doc=JsonDocument.Parse(line);Assert.Equal(run.RunId,doc.RootElement.GetProperty("RunId").GetString());});
    }
}
