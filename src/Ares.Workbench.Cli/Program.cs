using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ares.Workbench.Application;
using Ares.Workbench.Domain;
using Ares.Workbench.Infrastructure;
using Ares.Workbench.Adapters.Codex;
using Ares.Workbench.Adapters.AgentFramework;

if(args.Length>0 && args[0]=="direct")return await Ares.Workbench.Cli.DirectCli.Run(args.Skip(1).ToArray());

if(args.Length!=1){Console.Error.WriteLine("Usage: Ares.Workbench.Cli <run-config.json>");return 2;}
var json=new JsonSerializerOptions{WriteIndented=true,PropertyNameCaseInsensitive=true};
json.Converters.Add(new JsonStringEnumConverter());
var settings=JsonSerializer.Deserialize<RunSettings>(File.ReadAllText(args[0]),json)??throw new ArgumentException("Missing settings.");
var workspace=settings.Workspace;
var events=new JsonlEventSink(workspace.ArtifactRoot);
var tasks=new InMemoryTaskStore();var runs=new InMemoryRunStore();
var tools=new ToolHost(workspace,new(settings.Dotnet,settings.Git,settings.BuildArtifacts,settings.Environment),events);
var agent=new ScenarioAgentInvoker(tools,settings);
var codex=new CodexCliExecutor(new(settings.Codex,settings.CodexHome,settings.Environment,settings.ProtectedFiles,settings.Model),events);
var handlers=new Dictionary<string,INodeHandler>{
    ["grounding"]=new DelegateNodeHandler(async(i,ct)=>{
        foreach(var file in settings.SelectedFiles)await tools.Read(i,file);
        return NodeResult.Success("Selected files grounded with source paths.");
    }),
    ["architecture"]=new AgentNodeHandler(agent),
    ["plan"]=new AgentNodeHandler(agent),
    ["codex"]=new CodexNodeHandler(codex),
    ["test"]=new DelegateNodeHandler((i,ct)=>tools.Test(i,settings.TestProject,ct,settings.TestFilter)),
    ["review"]=new AgentNodeHandler(agent),
    ["independent-verify"]=new DelegateNodeHandler((i,ct)=>tools.Test(i,settings.TestProject,ct,settings.ReviewFilter)),
    ["diff"]=new DelegateNodeHandler((i,ct)=>tools.Diff(i,ct)),
    ["deliver"]=new DelegateNodeHandler((i,ct)=>tools.Diff(i,ct)),
    ["human"]=new HumanNodeHandler()
};
var task=new TaskService(tasks).Create(settings.TaskId,settings.Title,settings.Goal,settings.Acceptance,settings.Constraints,settings.Risk,workspace);
var coordinator=new WorkflowCoordinator(tasks,runs,events,new MicrosoftAgentFrameworkBackend(),new FileContextBuilder(settings.SelectedFiles),handlers);
var run=await coordinator.StartAsync(task.TaskId,WorkflowCatalog.Create(task.Risk),workspace);
if(run.State==RunState.Waiting&&settings.InteractiveHuman)
{
    var pending=run.PendingHuman!;
    Console.WriteLine($"Approval requested for {pending.Scope}. Type APPROVE to approve; any other input rejects.");
    var answer=Console.ReadLine();
    run=await coordinator.SubmitHumanAsync(run.RunId,new(pending.RequestId,pending.Scope,pending.RunVersion,"LocalOwner",answer=="APPROVE","Explicit console decision"));
}
var output=new {run,task=coordinator.Task(task.TaskId),real_codex_run=run.State==RunState.Completed&&run.Executions.Any(x=>x.NodeId=="codex"&&x.Result.Outcome==NodeOutcome.Succeeded)?"VERIFIED":"NOT_VERIFIED",
    real_agent_run="NOT_VERIFIED",agent_mode="CSharp scripted scenario; review uses actual dotnet test",crash_recovery=false};
var summary=tools.Artifact("run-summary.json");
await File.WriteAllTextAsync(summary,JsonSerializer.Serialize(output,json));
Console.WriteLine(JsonSerializer.Serialize(output,json));
return run.State==RunState.Completed?0:run.State==RunState.Waiting?3:1;

public sealed record RunSettings(Workspace Workspace,string Dotnet,string Git,string Codex,string CodexHome,
    string BuildArtifacts,ImmutableDictionary<string,string> Environment,ImmutableArray<string> SelectedFiles,
    ImmutableArray<string> ProtectedFiles,string TestProject,string? TestFilter,string? ReviewFilter,
    string TaskId,string Title,string Goal,ImmutableArray<string> Acceptance,ImmutableArray<string> Constraints,
    Risk Risk,string? FirstPassInstruction=null,string? Model=null,bool InteractiveHuman=false);
public sealed class ScenarioAgentInvoker(ToolHost tools,RunSettings settings):IAgentInvoker
{
    public async ValueTask<NodeResult> InvokeAsync(NodeInvocation i,CancellationToken ct)
    {
        if(i.Node.NodeId=="review")
        {
            var diff=await tools.Diff(i,ct);
            if(diff.Outcome!=NodeOutcome.Succeeded)return diff;
            var review=await tools.Test(i,settings.TestProject,ct,settings.ReviewFilter);
            return review with {ArtifactRefs=review.ArtifactRefs.AddRange(diff.ArtifactRefs)};
        }
        if(i.Node.NodeId=="plan")return NodeResult.Success(settings.FirstPassInstruction??"Implement all acceptance criteria within allowed source paths. Preserve protected tests and configuration.");
        return NodeResult.Success("Scripted architecture scenario: scoped local edits, deterministic checks and explicit Human approval.");
    }
}
