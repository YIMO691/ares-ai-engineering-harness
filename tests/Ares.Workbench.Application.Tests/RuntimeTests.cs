using System.Collections.Immutable;
using Ares.Workbench.Application;
using Ares.Workbench.Domain;
using Ares.Workbench.Infrastructure;
using Ares.Workbench.Adapters.AgentFramework;
namespace Ares.Workbench.Application.Tests;

internal sealed class InProcessWorkflowBackend : IWorkflowBackend
{
    public string BackendId=>"in-process-test-only";
    public async Task ExecuteAsync(WorkflowDefinition definition,string entry,Func<string,CancellationToken,ValueTask<string?>> dispatch,
        Func<string,ValueTask> observe,CancellationToken ct)
    {
        string? next=entry;int steps=0;
        while(next is not null){ct.ThrowIfCancellationRequested();if(++steps>100)throw new InvalidOperationException();next=await dispatch(next,ct);}
    }
}
internal sealed class MemoryEvents : IEventSink
{
    public List<WorkbenchEvent> Items {get;}=[];
    public Action<WorkbenchEvent>? Observe;
    public ValueTask AppendAsync(WorkbenchEvent value,CancellationToken ct=default){Items.Add(value);Observe?.Invoke(value);return ValueTask.CompletedTask;}
}
internal sealed class EmptyContext : IContextBuilder
{
    public ValueTask<ContextPackage> BuildAsync(EngineeringTask task,Workspace workspace,WorkflowRun run,CancellationToken ct)=>
        ValueTask.FromResult(new ContextPackage(task,workspace,ImmutableDictionary<string,string>.Empty,[],run.Executions,task.Constraints,[]));
}
public class RuntimeTests
{
    private static (WorkflowCoordinator Runtime,Workspace Workspace,WorkflowDefinition Definition,MemoryEvents Events,List<string> Called) Make(bool af,Risk risk,Func<NodeInvocation,NodeResult>? behavior=null,IWorkflowBackend? custom=null)
    {
        var path=Path.Combine(Path.GetTempPath(),"ares-tests",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(path);
        var workspace=new Workspace("w",path,"HEAD",["."],path,"local");
        var tasks=new InMemoryTaskStore();var runs=new InMemoryRunStore();var events=new MemoryEvents();var called=new List<string>();
        new TaskService(tasks).Create("task-"+Guid.NewGuid().ToString("N"),"test","goal",["acceptance"],[],risk,workspace);
        // Select the explicit task below rather than relying on any runtime-specific fixed task id.
        var task=new TaskService(tasks).Create("t","test","goal",["acceptance"],[],risk,workspace);
        var definition=WorkflowCatalog.Create(task.Risk);
        var handlers=definition.Nodes.ToDictionary(n=>n.Handler,n=>(INodeHandler)new DelegateNodeHandler((i,ct)=>{
            called.Add(i.Node.NodeId);
            if(i.Node.Kind==NodeKind.Human)return new HumanNodeHandler().ExecuteAsync(i,ct);
            return ValueTask.FromResult(behavior?.Invoke(i)??NodeResult.Success());
        }));
        IWorkflowBackend backend=custom??(af?new MicrosoftAgentFrameworkBackend():new InProcessWorkflowBackend());
        return(new(tasks,runs,events,backend,new EmptyContext(),handlers),workspace,definition,events,called);
    }
    private static async Task Evidence(bool af,string name,WorkflowRun run,MemoryEvents events)
    {
        if(!af)return;
        var root=Environment.GetEnvironmentVariable("ARES_EVIDENCE");if(root is null)return;
        var path=WorkspacePolicy.Under(WorkspacePolicy.ValidateRoot(root),Path.Combine(root,"contract-scenarios",name));
        Directory.CreateDirectory(path);
        var options=new System.Text.Json.JsonSerializerOptions{WriteIndented=true};
        options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        await File.WriteAllTextAsync(Path.Combine(path,"run-summary.json"),System.Text.Json.JsonSerializer.Serialize(new{run,executor="scripted fixture; not real Codex"},options));
        await File.WriteAllLinesAsync(Path.Combine(path,"events.jsonl"),events.Items.Select(e=>System.Text.Json.JsonSerializer.Serialize(e)));
    }
    [Theory][InlineData(false)][InlineData(true)]
    public async Task StandardReworksWithinSameRun(bool af)
    {
        var h=Make(af,Risk.Standard,i=>i.Node.NodeId=="review"&&i.Run.ReworkCount==0?NodeResult.Rework("Fix whitespace"):NodeResult.Success());
        var r=await h.Runtime.StartAsync("t",h.Definition,h.Workspace);
        Assert.Equal(RunState.Completed,r.State);Assert.Equal(1,r.ReworkCount);
        Assert.Equal(TaskLifecycle.Delivered,h.Runtime.Task("t").Lifecycle);
        Assert.Equal(new[]{"grounding","plan","codex","test","review","codex","test","review","deliver"},h.Called);
        Assert.Single(h.Events.Items.Select(e=>e.RunId).Distinct());
        Assert.Equal(h.Definition.Version,r.WorkflowVersion);
        await Evidence(af,"standard-contract",r,h.Events);
    }
    [Theory][InlineData(false)][InlineData(true)]
    public async Task BudgetBlocksWithoutAnotherRun(bool af)
    {
        var h=Make(af,Risk.Standard,i=>i.Node.NodeId=="review"?NodeResult.Rework("Still invalid"):NodeResult.Success());
        var r=await h.Runtime.StartAsync("t",h.Definition,h.Workspace);
        Assert.Equal(RunState.Blocked,r.State);Assert.Equal(3,r.ReworkCount);
        Assert.Equal(4,h.Called.Count(x=>x=="codex"));Assert.DoesNotContain("deliver",h.Called);
        Assert.Single(h.Events.Items,e=>e.EventType=="ReworkBudgetExhausted");
    }
    [Theory][InlineData(false)][InlineData(true)]
    public async Task FastSkipsPlanAndReview(bool af)
    {
        var h=Make(af,Risk.Fast);var r=await h.Runtime.StartAsync("t",h.Definition,h.Workspace);
        Assert.Equal(RunState.Completed,r.State);Assert.Equal(new[]{"codex","test","diff","deliver"},h.Called);
        await Evidence(af,"fast",r,h.Events);
    }
    [Theory][InlineData(false)][InlineData(true)]
    public async Task CriticalWaitRequiresMatchingExplicitDecision(bool af)
    {
        var h=Make(af,Risk.Critical);var r=await h.Runtime.StartAsync("t",h.Definition,h.Workspace);
        Assert.Equal(RunState.Waiting,r.State);Assert.Contains("independent-verify",h.Called);Assert.DoesNotContain("deliver",h.Called);
        Assert.NotEqual(TaskLifecycle.Delivered,h.Runtime.Task("t").Lifecycle);
        await Evidence(af,"critical-wait",r,h.Events);
        var p=r.PendingHuman!;
        await Assert.ThrowsAsync<InvalidOperationException>(()=>h.Runtime.SubmitHumanAsync(r.RunId,new("wrong",p.Scope,p.RunVersion,"Owner",true,"ok")));
        await Assert.ThrowsAsync<InvalidOperationException>(()=>h.Runtime.SubmitHumanAsync(r.RunId,new(p.RequestId,p.Scope,p.RunVersion+1,"Owner",true,"ok")));
        var completed=await h.Runtime.SubmitHumanAsync(r.RunId,new(p.RequestId,p.Scope,p.RunVersion,"Owner",true,"Reviewed"));
        Assert.Equal(r.RunId,completed.RunId);Assert.Equal(RunState.Completed,completed.State);
        await Evidence(af,"critical-approved",completed,h.Events);
    }
    [Theory][InlineData(false)][InlineData(true)]
    public async Task InvalidResultDoesNotAdvance(bool af)
    {
        var h=Make(af,Risk.Standard,i=>NodeResult.Success() with {Contract="invalid"});
        var r=await h.Runtime.StartAsync("t",h.Definition,h.Workspace);
        Assert.Equal(RunState.Failed,r.State);Assert.Single(h.Called);Assert.NotEqual(TaskLifecycle.Delivered,h.Runtime.Task("t").Lifecycle);
    }
    [Theory][InlineData(false)][InlineData(true)]
    public async Task UnknownWriteEffectIsBlockedWithoutRetry(bool af)
    {
        var h=Make(af,Risk.Fast,i=>NodeResult.Fail("NETWORK","Unknown write",true,EffectStatus.Unknown));
        var r=await h.Runtime.StartAsync("t",h.Definition,h.Workspace);
        Assert.Equal(RunState.Blocked,r.State);Assert.Single(h.Called);
    }
    [Fact]public void RouterEscalatesIrreversibleOperations()=>Assert.Equal(Risk.Critical,RiskRouter.Select(Risk.Fast,["irreversible"]));
    [Fact]public async Task BackendCannotSkipToDelivery()
    {
        var h=Make(false,Risk.Standard,custom:new BadBackend());
        var r=await h.Runtime.StartAsync("t",h.Definition,h.Workspace);
        Assert.Equal(RunState.Failed,r.State);Assert.Empty(h.Called);Assert.NotEqual(TaskLifecycle.Delivered,h.Runtime.Task("t").Lifecycle);
    }
    [Fact]public async Task CompletionRequiresEveryDeclaredCheck()
    {
        var h=Make(false,Risk.Fast);
        var d=h.Definition with {Routes=[new("codex",NodeOutcome.Succeeded,"$complete")]};
        var r=await h.Runtime.StartAsync("t",d,h.Workspace);
        Assert.Equal(RunState.Failed,r.State);Assert.NotEqual(TaskLifecycle.Delivered,h.Runtime.Task("t").Lifecycle);
    }
    [Theory][InlineData(false)][InlineData(true)]
    public async Task CancellationAfterDeliveryHandlerCannotCommitDelivered(bool af)
    {
        using var cancellation=new CancellationTokenSource();
        var h=Make(af,Risk.Fast);
        h.Events.Observe=e=>{if(e.EventType=="NodeCompleted"&&e.NodeId=="deliver")cancellation.Cancel();};
        var run=await h.Runtime.StartAsync("t",h.Definition,h.Workspace,cancellation.Token);
        Assert.Equal(RunState.Cancelled,run.State);
        Assert.NotEqual(TaskLifecycle.Delivered,h.Runtime.Task("t").Lifecycle);
        Assert.DoesNotContain(h.Events.Items,e=>e.EventType=="WorkflowCompleted");
    }
    private sealed class BadBackend:IWorkflowBackend
    {
        public string BackendId=>"adversarial-test";
        public async Task ExecuteAsync(WorkflowDefinition d,string entry,Func<string,CancellationToken,ValueTask<string?>> dispatch,Func<string,ValueTask> observe,CancellationToken ct)=>_ = await dispatch("deliver",ct);
    }
}
