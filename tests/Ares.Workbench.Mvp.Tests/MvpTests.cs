using Ares.Workbench.Adapters.Codex;
using System.Collections.Immutable;
using Ares.Workbench.Application;
using Ares.Workbench.Adapters.AgentFramework;
using Ares.Workbench.Domain;
using Ares.Workbench.Infrastructure;
using Ares.Workbench.Web;
using Microsoft.Extensions.Logging.Abstractions;
namespace Ares.Workbench.Mvp.Tests;
public class MvpTests
{
    private static string Root(){var p=LocalPaths.Output(Path.Combine(Path.GetTempPath(),"mvp-tests",Guid.NewGuid().ToString("N")));Directory.CreateDirectory(p);return p;}
    private sealed class Workspaces(string root):IProjectWorkspace
    {
        public ProjectProfile Validate(ProjectProfile p)=>p;
        public Workspace Create(ProjectProfile p,string id){var a=Path.Combine(root,"artifacts",id);Directory.CreateDirectory(a);return new(p.ProjectId,p.RepoRoot,"HEAD",p.AllowedPaths,a,"local");}
    }
    private sealed class EmptyContext:IContextBuilder
    {
        public ValueTask<ContextPackage> BuildAsync(EngineeringTask t,Workspace w,WorkflowRun r,CancellationToken ct)=>ValueTask.FromResult(new ContextPackage(t,w,ImmutableDictionary<string,string>.Empty,[],r.Executions,t.Constraints,[]));
    }
    private sealed class Handler(Func<NodeInvocation,CancellationToken,ValueTask<NodeResult>> f):INodeHandler
    {public ValueTask<NodeResult> ExecuteAsync(NodeInvocation i,CancellationToken ct)=>f(i,ct);}
    private sealed class Factory(bool rework=false,bool alwaysRework=false):IRunHandlerFactory
    {
        public int Active;public int MaxActive;public bool Hold;public bool PolicyBlock;public bool ReviewBlock;
        public string Stamp="HEAD";public bool FailPrerequisite;
        public TaskCompletionSource Started=new(TaskCreationOptions.RunContinuationsAsynchronously);
        public ValueTask<string> WorkspaceStampAsync(ProjectProfile p,Workspace w,CancellationToken ct)=>FailPrerequisite?ValueTask.FromException<string>(new InvalidOperationException("Environment unavailable")):ValueTask.FromResult(Stamp);
        public RunHandlers Create(ProjectProfile p,Workspace w)
        {
            async ValueTask<NodeResult> Execute(NodeInvocation i,CancellationToken ct)
            {
                if(i.Node.NodeId=="human")return await new HumanNodeHandler().ExecuteAsync(i,ct);
                if(i.Node.NodeId=="codex"){
                    if(PolicyBlock&&!p.AllowedPaths.Contains("tests"))
                        return NodeResult.Fail("PROJECT_POLICY","Task requires tests",false,EffectStatus.Unknown);
                    if(Hold){Started.TrySetResult();await Task.Delay(Timeout.Infinite,ct);}
                    var n=Interlocked.Increment(ref Active);MaxActive=Math.Max(MaxActive,n);
                    await Task.Delay(20,ct);Interlocked.Decrement(ref Active);
                }
                if(i.Node.NodeId=="review"&&ReviewBlock)return NodeResult.Fail("EXECUTOR_ENVIRONMENT","Reviewer unavailable",false,EffectStatus.Unknown);
                return i.Node.NodeId=="review"&&(alwaysRework||rework&&i.Run.ReworkCount==0)?NodeResult.Rework("Missing acceptance evidence"):NodeResult.Success("Verified fixture");
            }
            return new(new EmptyContext(),new[]{"grounding","plan","prepare","codex","test","review","human","deliver"}.ToDictionary(x=>x,x=>(INodeHandler)new Handler(Execute)));
        }
    }
    private sealed record Harness(string Root,SqliteWorkbenchStore Store,RunQueue Queue,WorkbenchService Service,Factory Factory,ProjectProfile Project);
    private static Harness Make(bool rework=false,bool always=false)
    {
        var root=Root();var store=new SqliteWorkbenchStore(root);var queue=new RunQueue();var factory=new Factory(rework,always);
        var service=new WorkbenchService(store,new Workspaces(root),factory,new MicrosoftAgentFrameworkBackend(),queue);
        var now=DateTimeOffset.UtcNow;var p=service.SaveProject(new(Guid.NewGuid().ToString("N"),"Demo",root,Risk.Standard,"dotnet build","dotnet test",["."],[],now,now));
        return new(root,store,queue,service,factory,p);
    }
    private static EngineeringTask NewTask(Harness h,Risk risk)=>h.Service.CreateTask(h.Project.ProjectId,"Task","Goal",["Acceptance"],[],risk);
    [Fact]public async Task SameRunReworkAndCompletedHistorySurviveNewStore()
    {
        var h=Make(true);var task=NewTask(h,Risk.Standard);string id=h.Service.RequestRun(task.TaskId);
        await h.Service.ProcessAsync(await h.Queue.Reader.ReadAsync(),default);
        var store=new SqliteWorkbenchStore(h.Root);var run=store.FindRun(id)!;
        Assert.Equal(RunState.Completed,run.State);Assert.Equal(1,run.ReworkCount);
        Assert.Equal(TaskLifecycle.Delivered,store.Task(task.TaskId).Lifecycle);
        Assert.Single(store.Projects());Assert.Single(store.Tickets());Assert.Single(store.Events(id),e=>e.EventType=="ReworkRequested");
        Assert.All(store.Events(id),e=>Assert.Equal(id,e.RunId));
        Assert.DoesNotContain(run.Executions,e=>e.NodeId=="architecture"||e.NodeId=="independent-verify");
        Assert.NotEmpty(store.Artifacts(id));
    }
    [Fact]public async Task CriticalWaitsForVersionBoundApproval()
    {
        var h=Make();var task=NewTask(h,Risk.Critical);var id=h.Service.RequestRun(task.TaskId);
        await h.Service.ProcessAsync(await h.Queue.Reader.ReadAsync(),default);
        var run=h.Store.FindRun(id)!;var p=run.PendingHuman!;
        Assert.Equal(RunState.Waiting,run.State);Assert.NotEqual(TaskLifecycle.Delivered,h.Store.Task(task.TaskId).Lifecycle);
        Assert.Throws<InvalidOperationException>(()=>h.Service.RequestApproval(id,p.RequestId,p.RunVersion+1,true,"bad"));
        h.Service.RequestApproval(id,p.RequestId,p.RunVersion,true,"Reviewed");
        Assert.Throws<InvalidOperationException>(()=>h.Service.RequestApproval(id,p.RequestId,p.RunVersion,true,"duplicate"));
        await h.Service.ProcessAsync(await h.Queue.Reader.ReadAsync(),default);
        Assert.Equal(RunState.Completed,h.Store.FindRun(id)!.State);
        Assert.Equal(TaskLifecycle.Delivered,h.Store.Task(task.TaskId).Lifecycle);
        Assert.Equal("Approved",new SqliteWorkbenchStore(h.Root).Approvals(id).Single().Status);
    }
    [Fact]public async Task CriticalRejectionBlocksDelivery()
    {
        var h=Make();var t=NewTask(h,Risk.Critical);var id=h.Service.RequestRun(t.TaskId);
        await h.Service.ProcessAsync(await h.Queue.Reader.ReadAsync(),default);
        var p=h.Store.FindRun(id)!.PendingHuman!;
        h.Service.RequestApproval(id,p.RequestId,p.RunVersion,false,"Needs changes");
        await h.Service.ProcessAsync(await h.Queue.Reader.ReadAsync(),default);
        Assert.Equal(RunState.Blocked,h.Store.FindRun(id)!.State);
        Assert.NotEqual(TaskLifecycle.Delivered,h.Store.Task(t.TaskId).Lifecycle);
    }
    [Fact]public async Task RestartInterruptsWaitingAndQueuedWithoutResume()
    {
        var h=Make();var t=NewTask(h,Risk.Critical);var id=h.Service.RequestRun(t.TaskId);
        await h.Service.ProcessAsync(await h.Queue.Reader.ReadAsync(),default);
        var fresh=new SqliteWorkbenchStore(h.Root);
        var service=new WorkbenchService(fresh,new Workspaces(h.Root),new Factory(),new MicrosoftAgentFrameworkBackend(),new RunQueue());
        await service.RecoverInterruptedAsync();
        Assert.Equal(RunState.Blocked,fresh.FindRun(id)!.State);Assert.Null(fresh.FindRun(id)!.PendingHuman);
        Assert.Single(fresh.Events(id),e=>e.EventType=="RunInterruptedByRestart");
        Assert.Equal("Interrupted",fresh.Approvals(id).Single().Status);
        var retry=service.RequestRun(t.TaskId);
        await service.RecoverInterruptedAsync();
        Assert.Equal("Interrupted",fresh.Ticket(retry).State);
        Assert.Null(fresh.FindRun(retry));
    }
    [Fact]public async Task ReworkBudgetIsTwoAndCannotDeliverOnFailure()
    {
        var h=Make(always:true);var t=NewTask(h,Risk.Standard);var id=h.Service.RequestRun(t.TaskId);
        await h.Service.ProcessAsync(await h.Queue.Reader.ReadAsync(),default);
        var r=h.Store.FindRun(id)!;
        Assert.Equal(2,r.ReworkCount);Assert.Equal(3,r.Executions.Count(e=>e.NodeId=="review"));
        Assert.Equal(RunState.Blocked,r.State);Assert.NotEqual(TaskLifecycle.Delivered,h.Store.Task(t.TaskId).Lifecycle);
    }
    [Fact]public async Task BackgroundWorkerSerializesDifferentProjects()
    {
        var h=Make();var t1=NewTask(h,Risk.Fast);
        var p=h.Project with {ProjectId=Guid.NewGuid().ToString("N"),Name="Second"};h.Service.SaveProject(p);
        var t2=h.Service.CreateTask(p.ProjectId,"Second","Goal",["Done"],[],Risk.Fast);
        var r1=h.Service.RequestRun(t1.TaskId);var r2=h.Service.RequestRun(t2.TaskId);
        using var worker=new RunWorker(h.Queue,h.Service,NullLogger<RunWorker>.Instance);
        await worker.StartAsync(default);
        for(int n=0;n<100 && h.Store.FindRun(r2)?.State!=RunState.Completed;n++)await Task.Delay(25);
        await worker.StopAsync(default);
        Assert.Equal(RunState.Completed,h.Store.FindRun(r1)!.State);
        Assert.Equal(RunState.Completed,h.Store.FindRun(r2)!.State);Assert.Equal(1,h.Factory.MaxActive);
    }
    [Fact]public void DuplicateRunAndActiveProjectEditAreRejected()
    {
        var h=Make();var t=NewTask(h,Risk.Fast);h.Service.RequestRun(t.TaskId);
        Assert.Throws<InvalidOperationException>(()=>h.Service.RequestRun(t.TaskId));
        Assert.Throws<InvalidOperationException>(()=>h.Service.SaveProject(h.Project with {Name="Changed"}));
    }
    [Fact]public void CommandsPreserveQuotedArgumentsAndRejectShellOperators()
    {
        var s=CommandLineSpec.Parse("\"C:/Program Files/tool.exe\" test \"space argument\"");
        Assert.Equal("C:/Program Files/tool.exe",s.Executable);Assert.Equal(["test","space argument"],s.Arguments);
        foreach(var command in new[]{"dotnet test; echo bad","dotnet test | more","dotnet test > out","dotnet test && exit"})
            Assert.Throws<ArgumentException>(()=>CommandLineSpec.Parse(command));
    }
    [Fact]public void ProjectBoundaryRejectsTraversalAndInstructionOutsideRepository()
    {
        var root=Root();var now=DateTimeOffset.UtcNow;
        var profile=new ProjectProfile("p","Demo",root,Risk.Standard,"dotnet build","dotnet test",["../"],[],now,now);
        var service=new ProjectWorkspace(root);
        Assert.Throws<UnauthorizedAccessException>(()=>service.Validate(profile));
        Assert.Throws<ArgumentException>(()=>service.Validate(profile with {AllowedPaths=["."],InstructionFiles=["../AGENTS.md"]}));
        Assert.Throws<UnauthorizedAccessException>(()=>LocalPaths.Output("C:/temp/ares"));
    }
    [Fact]public async Task StopInterruptsAndResumeReusesCompletedPrepareInSameRun()
    {
        var h=Make();h.Factory.Hold=true;
        var t=NewTask(h,Risk.Standard);var id=h.Service.RequestRun(t.TaskId);
        var running=h.Service.ProcessAsync(await h.Queue.Reader.ReadAsync(),default);
        await h.Factory.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        h.Service.RequestStop(id,false);await running;
        var paused=h.Store.FindRun(id)!;
        Assert.Equal(RunState.Paused,paused.State);
        Assert.Equal(NodeOutcome.Interrupted,paused.Executions.Last().Result.Outcome);
        Assert.NotEqual(TaskLifecycle.Delivered,h.Store.Task(t.TaskId).Lifecycle);
        Assert.Equal("codex",h.Store.Checkpoint(id)!.ResumeTarget);
        Assert.Contains("prepare",h.Store.Checkpoint(id)!.CompletedSteps);
        h.Factory.Hold=false;h.Service.RequestResume(id);
        Assert.Throws<InvalidOperationException>(()=>h.Service.RequestResume(id));
        await h.Service.ProcessAsync(await h.Queue.Reader.ReadAsync(),default);
        var done=h.Store.FindRun(id)!;
        Assert.Equal(RunState.Completed,done.State);
        Assert.Single(done.Executions,e=>e.NodeId=="prepare");
        Assert.Equal(2,done.Executions.Count(e=>e.NodeId=="codex"));
        Assert.Single(h.Store.Tickets());
    }
    [Fact]public async Task CancelRunningCannotResumeOrDeliver()
    {
        var h=Make();h.Factory.Hold=true;var t=NewTask(h,Risk.Standard);
        var id=h.Service.RequestRun(t.TaskId);
        var running=h.Service.ProcessAsync(await h.Queue.Reader.ReadAsync(),default);
        await h.Factory.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        h.Service.RequestStop(id,true);await running;
        Assert.Equal(RunState.Cancelled,h.Store.FindRun(id)!.State);
        Assert.False(h.Service.CanResume(id));
        Assert.Throws<InvalidOperationException>(()=>h.Service.RequestResume(id));
        Assert.NotEqual(TaskLifecycle.Delivered,h.Store.Task(t.TaskId).Lifecycle);
        Assert.DoesNotContain(h.Store.FindRun(id)!.Executions,e=>e.NodeId=="deliver");
    }
    [Fact]public async Task ProjectPolicyFixContinuesSameRunWithoutNewPreparation()
    {
        var h=Make();h.Factory.PolicyBlock=true;h.Service.SaveProject(h.Project with {AllowedPaths=["src"]});
        var t=NewTask(h,Risk.Standard);var id=h.Service.RequestRun(t.TaskId);
        await h.Service.ProcessAsync(await h.Queue.Reader.ReadAsync(),default);
        Assert.Equal(RunState.Blocked,h.Store.FindRun(id)!.State);
        Assert.Equal("PROJECT_POLICY",h.Store.Checkpoint(id)!.BlockedCategory);
        h.Service.SaveProject(h.Project with {AllowedPaths=["src","tests"],TestCommand="dotnet test changed"});
        h.Service.RequestResume(id);await h.Service.ProcessAsync(await h.Queue.Reader.ReadAsync(),default);
        Assert.Equal(RunState.Completed,h.Store.FindRun(id)!.State);
        Assert.Single(h.Store.FindRun(id)!.Executions,e=>e.NodeId=="prepare");
        Assert.Single(h.Store.Runs());
    }
    [Theory][InlineData("goal")][InlineData("acceptance")][InlineData("root")]
    public async Task MaterialChangesRejectResume(string change)
    {
        var h=Make();h.Factory.PolicyBlock=true;var t=NewTask(h,Risk.Standard);
        var id=h.Service.RequestRun(t.TaskId);await h.Service.ProcessAsync(await h.Queue.Reader.ReadAsync(),default);
        if(change=="root")h.Service.SaveProject(h.Project with {RepoRoot=h.Root+"-other"});
        else ((ITaskStore)h.Store).Update(h.Store.Task(t.TaskId) with {
            Goal=change=="goal"?"Changed":t.Goal,Acceptance=change=="acceptance"?["Changed"]:t.Acceptance});
        Assert.Throws<InvalidOperationException>(()=>h.Service.RequestResume(id));
        Assert.Single(h.Store.Runs());
    }
    [Fact]public async Task GitRefChangeBlocksWorkerBeforeAnyRedispatch()
    {
        var h=Make();h.Factory.PolicyBlock=true;var t=NewTask(h,Risk.Standard);
        var id=h.Service.RequestRun(t.TaskId);await h.Service.ProcessAsync(await h.Queue.Reader.ReadAsync(),default);
        var count=h.Store.FindRun(id)!.Executions.Length;
        h.Factory.Stamp="changed HEAD";h.Service.RequestResume(id);
        await h.Service.ProcessAsync(await h.Queue.Reader.ReadAsync(),default);
        Assert.Equal("RESUME_INVALID",h.Store.FindRun(id)!.Failure!.Code);
        Assert.False(h.Service.CanResume(id));Assert.Equal(count,h.Store.FindRun(id)!.Executions.Length);
    }
    [Fact]public async Task CancelQueuedResumePreventsExecution()
    {
        var h=Make();h.Factory.PolicyBlock=true;var t=NewTask(h,Risk.Standard);
        var id=h.Service.RequestRun(t.TaskId);await h.Service.ProcessAsync(await h.Queue.Reader.ReadAsync(),default);
        h.Service.RequestResume(id);h.Service.RequestStop(id,true);
        await h.Service.ProcessAsync(await h.Queue.Reader.ReadAsync(),default);
        Assert.Equal(RunState.Cancelled,h.Store.FindRun(id)!.State);
        Assert.Single(h.Store.FindRun(id)!.Executions,e=>e.NodeId=="prepare");
        Assert.False(h.Service.CanResume(id));
    }
    [Fact]public async Task CancelWaitingInvalidatesHumanApproval()
    {
        var h=Make();var t=NewTask(h,Risk.Critical);var id=h.Service.RequestRun(t.TaskId);
        await h.Service.ProcessAsync(await h.Queue.Reader.ReadAsync(),default);var p=h.Store.FindRun(id)!.PendingHuman!;
        h.Service.RequestStop(id,true);
        Assert.Throws<InvalidOperationException>(()=>h.Service.RequestApproval(id,p.RequestId,p.RunVersion,true,"stale"));
        Assert.Equal(RunState.Cancelled,h.Store.FindRun(id)!.State);Assert.Equal("Cancelled",h.Store.Approvals(id).Single().Status);
    }
    [Fact]public async Task PrerequisiteFailureCanBeFixedAndResumedWithoutNewRun()
    {
        var h=Make();h.Factory.FailPrerequisite=true;var t=NewTask(h,Risk.Standard);
        var id=h.Service.RequestRun(t.TaskId);await h.Service.ProcessAsync(await h.Queue.Reader.ReadAsync(),default);
        Assert.Equal(RunState.Blocked,h.Store.FindRun(id)!.State);Assert.True(h.Service.CanResume(id));
        Assert.Empty(h.Store.FindRun(id)!.Executions);h.Factory.FailPrerequisite=false;
        h.Service.RequestResume(id);await h.Service.ProcessAsync(await h.Queue.Reader.ReadAsync(),default);
        Assert.Equal(RunState.Completed,h.Store.FindRun(id)!.State);Assert.Single(h.Store.Runs());
    }
    [Fact]public async Task ChangedVerificationCommandRetestsButKeepsPreparation()
    {
        var h=Make();h.Factory.ReviewBlock=true;var t=NewTask(h,Risk.Standard);
        var id=h.Service.RequestRun(t.TaskId);await h.Service.ProcessAsync(await h.Queue.Reader.ReadAsync(),default);
        Assert.Equal("review",h.Store.FindRun(id)!.CurrentNode);
        h.Service.SaveProject(h.Project with {TestCommand="dotnet test updated"});
        h.Factory.ReviewBlock=false;h.Service.RequestResume(id);
        Assert.Equal("test",h.Store.Checkpoint(id)!.ResumeTarget);
        await h.Service.ProcessAsync(await h.Queue.Reader.ReadAsync(),default);
        var done=h.Store.FindRun(id)!;Assert.Equal(RunState.Completed,done.State);
        Assert.Equal(2,done.Executions.Count(e=>e.NodeId=="test"));Assert.Single(done.Executions,e=>e.NodeId=="prepare");
    }
    [Fact]public void NativeReferenceParserUsesActualThreadStartedEvent()
    {
        const string id="01a07f38-1365-7ea0-bd24-173fe0ac4998";
        Assert.Equal(id,Ares.Workbench.Adapters.Codex.CodexRoleExecutor.NativeThreadReference(
            "noise\n"+System.Text.Json.JsonSerializer.Serialize(new{type="thread.started",thread_id=id})+"\n"+System.Text.Json.JsonSerializer.Serialize(new{type="turn.started"})));
        Assert.Null(Ares.Workbench.Adapters.Codex.CodexRoleExecutor.NativeThreadReference("{\"type\":\"thread.started\",\"thread_id\":\"bad\"}"));
    }

}
