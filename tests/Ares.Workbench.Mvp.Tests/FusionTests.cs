using System.Collections.Immutable;
using System.Text.Json;
using Ares.Workbench.Application;
using Ares.Workbench.Adapters.AgentFramework;
using Ares.Workbench.Domain;
using Ares.Workbench.Infrastructure;
using Ares.Workbench.Web;
namespace Ares.Workbench.Mvp.Tests;

public class FusionTests
{
    private static readonly ReadyAnchors Anchors=new("Trim labels",["null returns empty","trim outer whitespace"],"No API changes","src only","Use Trim","dotnet test",[]);
    private sealed class Workspaces(string root):IProjectWorkspace {
        public ProjectProfile Validate(ProjectProfile p)=>p;
        public Workspace Create(ProjectProfile p,string id){var dir=LocalPaths.Output(Path.Combine(root,"artifacts",id));Directory.CreateDirectory(dir);return new(p.ProjectId,p.RepoRoot,"HEAD",p.AllowedPaths,dir,"local");}
    }
    private sealed class Context:IContextBuilder {
        public ValueTask<ContextPackage> BuildAsync(EngineeringTask t,Workspace w,WorkflowRun r,CancellationToken ct)=>ValueTask.FromResult(new ContextPackage(t,w,ImmutableDictionary<string,string>.Empty,[],r.Executions,t.Constraints,[]));
    }
    private sealed class Handler(Func<NodeInvocation,CancellationToken,ValueTask<NodeResult>> f):INodeHandler {
        public ValueTask<NodeResult> ExecuteAsync(NodeInvocation i,CancellationToken ct)=>f(i,ct);
    }
    private sealed class Factory(SqliteWorkbenchStore store):IRunHandlerFactory {
        public readonly string Primary=Guid.NewGuid().ToString();
        public readonly string Reviewer=Guid.NewGuid().ToString();
        public List<string?> PrimaryCalls=[];
        public ReadyAnchors Proposal=Anchors;
        public bool Hold,Rework,BlockReview,MissingSession;
        public string Stamp="HEAD";
        public TaskCompletionSource Started=new(TaskCreationOptions.RunContinuationsAsynchronously);
        public ValueTask<string> WorkspaceStampAsync(ProjectProfile p,Workspace w,CancellationToken ct)=>ValueTask.FromResult(Stamp);
        public RunHandlers Create(ProjectProfile p,Workspace w) {
            async ValueTask<NodeResult> Execute(NodeInvocation i,CancellationToken ct) {
                var cp=store.Checkpoint(i.Run.RunId)!;
                if(i.Node.NodeId=="discuss") {
                    PrimaryCalls.Add(cp.PrimarySessionRef);
                    if(!MissingSession)store.UpdateCheckpoint(i.Run.RunId,c=>c with {PrimarySessionRef=Primary});
                    return NodeResult.Success(JsonSerializer.Serialize(new {content="Discussed with Owner",ready=Proposal}));
                }
                if(i.Node.NodeId=="codex") {
                    PrimaryCalls.Add(cp.PrimarySessionRef);
                    if(Hold){Started.TrySetResult();await Task.Delay(Timeout.Infinite,ct);}
                }
                if(i.Node.NodeId=="review") {
                    store.UpdateCheckpoint(i.Run.RunId,c=>c with {ReviewerSessionRef=Reviewer});
                    if(BlockReview)return NodeResult.Fail("EXECUTOR_ENVIRONMENT","Reviewer unavailable",false,EffectStatus.Unknown);
                    if(Rework&&i.Run.ReworkCount==0)return NodeResult.Rework("Fix missing requirement");
                }
                if(i.Node.NodeId=="human")return await new HumanNodeHandler().ExecuteAsync(i,ct);
                return NodeResult.Success("Verified fixture");
            }
            return new(new Context(),new[]{"discuss","codex","test","review","human","deliver"}.ToDictionary(n=>n,n=>(INodeHandler)new Handler(Execute)));
        }
    }
    private sealed class Harness {
        public string Root;public SqliteWorkbenchStore Store;public RunQueue Queue=new();public Factory Factory;public WorkbenchService Service;public EngineeringTask Task;
        public Harness(Risk risk=Risk.Standard) {
            Root=LocalPaths.Output(Path.Combine(Path.GetTempPath(),"fusion-tests",Guid.NewGuid().ToString("N")));Directory.CreateDirectory(Root);
            Store=new(Root);Factory=new(Store);Service=new(Store,new Workspaces(Root),Factory,new MicrosoftAgentFrameworkBackend(),Queue);
            var now=DateTimeOffset.UtcNow;Service.SaveProject(new("p","Demo",Root,risk,"dotnet build","dotnet test",["src"],[],now,now));
            Task=Service.CreateDiscussingTask("p","Task","",[],[],risk);
        }
        public EngineeringTask Current=>Store.Task(Task.TaskId);
        public async Task Discuss() {
            Service.RequestDiscussion(Task.TaskId,Current.Fusion!.Revision,"Please discuss the requirements");
            await Service.ProcessAsync(await Queue.Reader.ReadAsync(),default);
        }
        public async Task<string> Run() {
            if(Current.Lifecycle==TaskLifecycle.Discussing){await Discuss();Service.FreezeReady(Task.TaskId,Current.Fusion!.Revision,Anchors);}
            var id=Service.RequestRun(Task.TaskId);await Service.ProcessAsync(await Queue.Reader.ReadAsync(),default);return id;
        }
    }
    [Fact]public void NewTaskStartsDiscussingAndSurvivesDatabaseReload() {
        var h=new Harness();var task=new SqliteWorkbenchStore(h.Root).Task(h.Task.TaskId);
        Assert.Equal(TaskLifecycle.Discussing,task.Lifecycle);Assert.NotNull(task.Fusion);Assert.Empty(task.Fusion.Turns);
        Assert.Throws<InvalidOperationException>(()=>h.Service.RequestRun(task.TaskId));
    }
    [Fact]public async Task DiscussionBindsAndResumesOneNativeReference() {
        var h=new Harness();await h.Discuss();await h.Discuss();
        Assert.Equal(new string?[]{null,h.Factory.Primary},h.Factory.PrimaryCalls);
        Assert.Equal(4,h.Current.Fusion!.Turns.Length);
        Assert.Equal(h.Factory.Primary,new SqliteWorkbenchStore(h.Root).Task(h.Task.TaskId).Fusion!.PrimarySessionRef);
    }
    [Fact]public async Task MissingNativeBindingCannotReady() {
        var h=new Harness();h.Factory.MissingSession=true;await h.Discuss();
        Assert.Equal("Blocked",h.Current.Fusion!.DiscussionState);
        Assert.Throws<InvalidOperationException>(()=>h.Service.FreezeReady(h.Task.TaskId,h.Current.Fusion.Revision,Anchors));
    }
    [Fact]public async Task UnresolvedQuestionsDenyReadyAndDoNotQueueRun() {
        var h=new Harness();await h.Discuss();
        Assert.Throws<InvalidOperationException>(()=>h.Service.FreezeReady(h.Task.TaskId,h.Current.Fusion!.Revision,Anchors with {Unresolved=["Which API?"]}));
        Assert.Empty(h.Store.Tickets());Assert.Equal(TaskLifecycle.Discussing,h.Current.Lifecycle);
    }
    [Fact]public async Task FrozenSnapshotCannotBeEditedAndStaleRevisionIsRejected() {
        var h=new Harness();await h.Discuss();var revision=h.Current.Fusion!.Revision;
        h.Service.SaveReadyDraft(h.Task.TaskId,revision,Anchors);
        Assert.Throws<InvalidOperationException>(()=>h.Service.FreezeReady(h.Task.TaskId,revision,Anchors));
        h.Service.FreezeReady(h.Task.TaskId,h.Current.Fusion!.Revision,Anchors);
        Assert.Equal(1,h.Current.Fusion!.Frozen!.Version);
        Assert.Throws<InvalidOperationException>(()=>h.Service.SaveReadyDraft(h.Task.TaskId,h.Current.Fusion.Revision,Anchors with {Goal="Other"}));
        Assert.Equal(Anchors,new SqliteWorkbenchStore(h.Root).Task(h.Task.TaskId).Fusion!.Frozen!.Anchors with {Acceptance=Anchors.Acceptance,Unresolved=Anchors.Unresolved});
    }
    [Theory][InlineData(Risk.Fast)][InlineData(Risk.Standard)][InlineData(Risk.Critical)]
    public void FusionDefinitionsBeginAtImplementationWithoutPreparationAgents(Risk risk) {
        var d=MvpWorkflowCatalog.CreateFusion(risk);Assert.Equal("codex",d.EntryNode);
        Assert.DoesNotContain(d.Nodes,n=>n.NodeId is "prepare" or "grounding" or "plan");
        Assert.Equal(risk!=Risk.Fast,d.Nodes.Any(n=>n.NodeId=="review"));
        Assert.Contains(d.Routes,r=>r.From=="test"&&r.Outcome==NodeOutcome.ReworkRequired&&r.Target=="codex");
    }
    [Fact]public async Task ReadyImplementationReusesDiscussionAndWaitsForOwnerAcceptance() {
        var h=new Harness();var id=await h.Run();
        Assert.Equal(new string?[]{null,h.Factory.Primary},h.Factory.PrimaryCalls);
        Assert.Equal(TaskLifecycle.AwaitingAcceptance,h.Current.Lifecycle);
        Assert.NotEqual(h.Store.Checkpoint(id)!.PrimarySessionRef,h.Store.Checkpoint(id)!.ReviewerSessionRef);
        Assert.DoesNotContain(h.Store.FindRun(id)!.Executions,e=>e.NodeId=="prepare");
        h.Service.OwnerAcceptance(h.Task.TaskId,id,1,true,"Checked every criterion");
        Assert.Equal(TaskLifecycle.Done,new SqliteWorkbenchStore(h.Root).Task(h.Task.TaskId).Lifecycle);
        Assert.Throws<InvalidOperationException>(()=>h.Service.OwnerAcceptance(h.Task.TaskId,id,1,true,"Duplicate"));
    }
    [Fact]public async Task RejectNeverDoneAndNextReadyPreservesPriorSnapshot() {
        var h=new Harness();var id=await h.Run();
        Assert.Throws<ArgumentException>(()=>h.Service.OwnerAcceptance(h.Task.TaskId,id,1,false,""));
        h.Service.OwnerAcceptance(h.Task.TaskId,id,1,false,"Missing edge case");
        Assert.Equal(TaskLifecycle.Discussing,h.Current.Lifecycle);
        Assert.Throws<InvalidOperationException>(()=>h.Service.FreezeReady(h.Task.TaskId,h.Current.Fusion!.Revision,Anchors));
        await h.Discuss();
        h.Service.FreezeReady(h.Task.TaskId,h.Current.Fusion!.Revision,Anchors with {KeyDecisions="Additional edge case"});
        Assert.Equal(2,h.Current.Fusion!.Snapshots.Length);
        Assert.Equal(Anchors.KeyDecisions,h.Current.Fusion.Snapshots[0].Anchors.KeyDecisions);
        Assert.Equal(h.Factory.Primary,h.Current.Fusion.PrimarySessionRef);
        Assert.Throws<InvalidOperationException>(()=>h.Service.OwnerAcceptance(h.Task.TaskId,id,1,true,"Stale"));
    }
    [Fact]public async Task ReviewerReworkContinuesOriginalPrimaryAndRetests() {
        var h=new Harness();h.Factory.Rework=true;var id=await h.Run();var run=h.Store.FindRun(id)!;
        Assert.Equal(1,run.ReworkCount);Assert.Equal(2,run.Executions.Count(e=>e.NodeId=="test"));
        Assert.All(h.Factory.PrimaryCalls.Skip(1),id=>Assert.Equal(h.Factory.Primary,id));
        Assert.Equal(TaskLifecycle.AwaitingAcceptance,h.Current.Lifecycle);
    }
    [Fact]public async Task StopResumeKeepsNativeReferenceAndReadySnapshot() {
        var h=new Harness();await h.Discuss();h.Service.FreezeReady(h.Task.TaskId,h.Current.Fusion!.Revision,Anchors);
        h.Factory.Hold=true;var id=h.Service.RequestRun(h.Task.TaskId);
        var running=h.Service.ProcessAsync(await h.Queue.Reader.ReadAsync(),default);await h.Factory.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        h.Service.RequestStop(id,false);await running;
        Assert.Equal(RunState.Paused,h.Store.FindRun(id)!.State);
        h.Factory.Hold=false;h.Service.RequestResume(id);await h.Service.ProcessAsync(await h.Queue.Reader.ReadAsync(),default);
        Assert.Equal(RunState.Completed,h.Store.FindRun(id)!.State);
        Assert.All(h.Factory.PrimaryCalls.Skip(1),id=>Assert.Equal(h.Factory.Primary,id));
        Assert.Single(h.Current.Fusion!.Snapshots);
    }
    [Fact]public async Task ChangedFrozenBoundaryRejectsResume() {
        var h=new Harness();h.Factory.BlockReview=true;var id=await h.Run();
        h.Service.SaveProject(h.Store.Project("p") with {AllowedPaths=["."]});
        Assert.Throws<InvalidOperationException>(()=>h.Service.RequestResume(id));
        Assert.NotEqual(TaskLifecycle.Done,h.Current.Lifecycle);
    }
    [Fact]public async Task GitStateChangeAfterReadyBlocksBeforeImplementation() {
        var h=new Harness();await h.Discuss();h.Service.FreezeReady(h.Task.TaskId,h.Current.Fusion!.Revision,Anchors);h.Factory.Stamp="other HEAD";
        var id=await h.Run();Assert.Equal("RESUME_INVALID",h.Store.FindRun(id)!.Failure!.Code);
        Assert.Single(h.Factory.PrimaryCalls);Assert.NotEqual(TaskLifecycle.Done,h.Current.Lifecycle);
    }
    [Fact]public async Task CriticalApprovalAndOwnerAcceptanceAreSeparate() {
        var h=new Harness(Risk.Critical);var id=await h.Run();var pending=h.Store.FindRun(id)!.PendingHuman!;
        Assert.Equal(TaskLifecycle.Active,h.Current.Lifecycle);
        h.Service.RequestApproval(id,pending.RequestId,pending.RunVersion,true,"Approve gate");
        await h.Service.ProcessAsync(await h.Queue.Reader.ReadAsync(),default);
        Assert.Equal(TaskLifecycle.AwaitingAcceptance,h.Current.Lifecycle);
        h.Service.OwnerAcceptance(h.Task.TaskId,id,1,true,"Accept result");Assert.Equal(TaskLifecycle.Done,h.Current.Lifecycle);
    }
    [Fact]public async Task BlockedRunCanReturnToDiscussionWithoutLosingHistory() {
        var h=new Harness();h.Factory.BlockReview=true;var id=await h.Run();
        h.Service.ReturnToDiscussion(h.Task.TaskId,h.Current.Fusion!.Revision);
        Assert.Equal(TaskLifecycle.Discussing,h.Current.Lifecycle);Assert.Single(h.Store.Runs());
        Assert.Throws<InvalidOperationException>(()=>h.Service.FreezeReady(h.Task.TaskId,h.Current.Fusion!.Revision,Anchors));
        await h.Discuss();h.Service.FreezeReady(h.Task.TaskId,h.Current.Fusion!.Revision,Anchors);
        Assert.Equal(2,h.Current.Fusion!.Snapshots.Length);Assert.Equal(RunState.Blocked,h.Store.FindRun(id)!.State);
    }
    [Fact]public async Task RestartedDiscussionRetainsBindingAndCanContinue() {
        var h=new Harness();await h.Discuss();
        h.Service.RequestDiscussion(h.Task.TaskId,h.Current.Fusion!.Revision,"Next question");
        await h.Service.RecoverInterruptedAsync();
        Assert.Equal("Blocked",h.Current.Fusion!.DiscussionState);
        Assert.Equal(h.Factory.Primary,h.Current.Fusion.PrimarySessionRef);
        await h.Service.ProcessAsync(await h.Queue.Reader.ReadAsync(),default);
        Assert.Equal(2,h.Current.Fusion.Turns.Count(t=>t.Speaker=="Owner"));
        await h.Discuss();Assert.Equal("Idle",h.Current.Fusion!.DiscussionState);
    }
    [Fact]public void DiscussionTitleIsEnoughWhenOptionalGoalIsEmpty() {
        var h=new Harness();
        var task=h.Service.CreateDiscussingTask("p","Clarify the request",null!,[],[],Risk.Standard);
        Assert.Equal(TaskLifecycle.Discussing,task.Lifecycle);Assert.Equal("",task.Goal);
    }

    [Fact]public void LoadedSqliteEngineIncludesCve20256965Fix() {
        var h=new Harness();
        using var connection=new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
        connection.Open();using var command=connection.CreateCommand();command.CommandText="select sqlite_version()";
        var version=Version.Parse((string)command.ExecuteScalar()!);
        Assert.True(version>=new Version(3,50,2),"Loaded SQLite "+version+" predates CVE-2025-6965 fix");
    }

}
