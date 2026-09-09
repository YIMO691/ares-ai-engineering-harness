using System.Collections.Immutable;
using Ares.Workbench.Domain;
using Ares.Workbench.Application;
using Ares.Workbench.Adapters.AgentFramework;
using Ares.Workbench.Infrastructure;
using Ares.Workbench.Web;
using Ares.Workbench.Web.Pages;
namespace Ares.Workbench.Mvp.Tests;

public class DirectTests
{
    private static readonly ReadyAnchors Anchors=new("Trim labels",["Trim outer whitespace"],"No API changes","src only","Use Trim","Run tests",[]);
    private static readonly OwnerEvidence Owner=new("Implement the agreed change","test-fixture/owner-message");
    private sealed class WorkspaceFactory(string root):IProjectWorkspace {
        public ProjectProfile Validate(ProjectProfile p)=>p;
        public Workspace Create(ProjectProfile p,string id) {
            var dir=LocalPaths.Output(Path.Combine(root,"artifacts",id));Directory.CreateDirectory(dir);
            return new(p.ProjectId,p.RepoRoot,"HEAD",p.AllowedPaths,dir,"local");
        }
    }
    private sealed class Context:IContextBuilder {
        public ValueTask<ContextPackage> BuildAsync(EngineeringTask t,Workspace w,WorkflowRun r,CancellationToken ct)=>
            ValueTask.FromResult(new ContextPackage(t,w,ImmutableDictionary<string,string>.Empty,[],r.Executions,t.Constraints,[]));
    }
    private sealed class Handler(Func<NodeInvocation,CancellationToken,ValueTask<NodeResult>> fn):INodeHandler {
        public ValueTask<NodeResult> ExecuteAsync(NodeInvocation i,CancellationToken ct)=>fn(i,ct);
    }
    private sealed class Factory:IRunHandlerFactory,IDirectEvidence {
        public Dictionary<string,string> Files=new(){{"src/code.cs","initial"}};
        public string Stamp="HEAD";
        public List<string> Calls=[];
        public bool FailTest,ReworkReview,BlockReview,MutateOnReview,Hold;
        public TaskCompletionSource Started=new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release=new(TaskCreationOptions.RunContinuationsAsynchronously);
        public ValueTask<SourceSnapshot> SnapshotAsync(ProjectProfile p,Workspace w,CancellationToken ct)=>ValueTask.FromResult(new SourceSnapshot(Stamp,Files.ToImmutableDictionary()));
        public ValueTask<string> DiffAsync(ProjectProfile p,Workspace w,CancellationToken ct)=>ValueTask.FromResult("actual fixture diff");
        public bool Writable(ProjectProfile p,Workspace w,string path)=>path.StartsWith("src/");
        public RunHandlers Create(ProjectProfile p,Workspace w) {
            async ValueTask<NodeResult> Execute(NodeInvocation i,CancellationToken ct) {
                Calls.Add(i.Node.NodeId);
                if(i.Node.NodeId=="test") {
                    if(Hold){Started.TrySetResult();await Release.Task.WaitAsync(ct);}
                    return FailTest?NodeResult.Rework("Missing whitespace handling"):NodeResult.Success("Tests passed");
                }
                if(i.Node.NodeId=="review") {
                    if(MutateOnReview)Files["src/code.cs"]="concurrent edit";
                    if(BlockReview)return NodeResult.Fail("EXECUTOR_ENVIRONMENT","Unavailable");
                    return ReworkReview?NodeResult.Rework("Handle tabs"):NodeResult.Success("Independent review passed");
                }
                throw new InvalidOperationException("Unexpected Primary invocation: "+i.Node.NodeId);
            }
            return new(new Context(),new[]{"test","review"}.ToDictionary(x=>x,x=>(INodeHandler)new Handler(Execute)));
        }
    }
    private sealed class Harness {
        public readonly string Root=LocalPaths.Output(Path.Combine(Path.GetTempPath(),"direct-tests",Guid.NewGuid().ToString("N")));
        public readonly SqliteWorkbenchStore Store;
        public readonly Factory Factory=new();
        public readonly WorkspaceFactory Workspaces;
        public readonly DirectWorkflowService Service;
        public EngineeringTask Task;
        public Harness(Risk risk=Risk.Standard) {
            Directory.CreateDirectory(Root);Store=new(Root);Workspaces=new(Root);
            Service=new(Store,Workspaces,Factory,Factory,new MicrosoftAgentFrameworkBackend());
            var now=DateTimeOffset.UtcNow;
            Store.SaveProject(new("p","Project",Root,risk,"dotnet build","dotnet test",["src"],[],now,now));
            Task=Service.Create("p","Direct task",risk,"existing native conversation",null);
        }
        public EngineeringTask Current=>Store.Task(Task.TaskId);
        public long Revision=>Current.Direct!.Revision;
        public async Task Ready()=>Task=await Service.Agree(Task.TaskId,Revision,Anchors,[new(1,"automatic","test")],Owner,default);
        public async Task Submit() {
            if(Current.Direct!.Stage==DirectStage.Discussing)await Ready();
            await Service.Begin(Task.TaskId,Revision,Current.Risk==Risk.Critical?Owner:null,default);
            Factory.Files["src/code.cs"]="implemented";
            await Service.Submit(Task.TaskId,Revision,"Changed Trim implementation",default);
        }
        public async Task Verify(){await Submit();await Service.Verify(Task.TaskId,Revision,default);}
    }
    [Theory][InlineData(Risk.Fast)][InlineData(Risk.Standard)][InlineData(Risk.Critical)]
    public async Task DirectWorkflowNeverInvokesAReplacementPrimary(Risk risk) {
        var h=new Harness(risk);await h.Verify();
        Assert.Equal(DirectStage.AwaitingAcceptance,h.Current.Direct!.Stage);
        Assert.Equal(risk==Risk.Fast?new[]{"test"}:new[]{"test","review"},h.Factory.Calls);
        Assert.Equal(TaskLifecycle.AwaitingAcceptance,h.Current.Lifecycle);
        Assert.Empty(h.Store.Approvals(h.Current.Direct.LastRunId!));
        await h.Service.Accept(h.Task.TaskId,h.Revision,new("Accepted all criteria","test-fixture/accept"),default);
        Assert.Equal(DirectStage.Done,new SqliteWorkbenchStore(h.Root).Task(h.Task.TaskId).Direct!.Stage);
    }
    [Fact]public async Task StaleCommandsAndIncompleteAcceptanceMappingsAreRejected() {
        var h=new Harness();await h.Ready();
        await Assert.ThrowsAsync<InvalidOperationException>(()=>h.Service.Begin(h.Task.TaskId,0,null,default));
        await h.Service.Feedback(h.Task.TaskId,h.Revision,"Expand scope",true,Owner);
        await Assert.ThrowsAsync<ArgumentException>(()=>h.Service.Agree(h.Task.TaskId,h.Revision,Anchors,[],Owner,default));
    }
    [Fact]public async Task CriticalApprovalPrecedesImplementation() {
        var h=new Harness(Risk.Critical);await h.Ready();
        await Assert.ThrowsAsync<ArgumentException>(()=>h.Service.Begin(h.Task.TaskId,h.Revision,null,default));
        Assert.Equal(DirectStage.Ready,h.Current.Direct!.Stage);
    }
    [Theory][InlineData(true)][InlineData(false)]
    public async Task TestAndReviewerDefectsReturnToExternalPrimaryWithoutChangingAgreement(bool test) {
        var h=new Harness();h.Factory.FailTest=test;h.Factory.ReworkReview=!test;await h.Verify();
        Assert.Equal(DirectStage.Rework,h.Current.Direct!.Stage);
        Assert.Single(h.Current.Direct.Agreements);
        h.Factory.FailTest=false;h.Factory.ReworkReview=false;await h.Verify();
        Assert.Equal(DirectStage.AwaitingAcceptance,h.Current.Direct!.Stage);
        Assert.Single(h.Current.Direct.Agreements);Assert.Equal(2,h.Store.Runs().Count);
    }
    [Fact]public async Task OwnerDefectAndScopeChangeUseDifferentPaths() {
        var h=new Harness();await h.Verify();
        await h.Service.Feedback(h.Task.TaskId,h.Revision,"Missing case",false,new("Fix the omission","test-fixture/feedback"));
        Assert.Equal(DirectStage.Rework,h.Current.Direct!.Stage);Assert.Single(h.Current.Direct.Agreements);
        await h.Service.Feedback(h.Task.TaskId,h.Revision,"New requirement",true,Owner);
        await h.Ready();Assert.Equal(2,h.Current.Direct!.Agreements.Length);
    }
    [Fact]public async Task SourceChangesBeforeImplementationRequireReconciliation() {
        var h=new Harness();await h.Ready();h.Factory.Files["src/code.cs"]="another task changed source";
        await Assert.ThrowsAsync<InvalidOperationException>(()=>h.Service.Begin(h.Task.TaskId,h.Revision,null,default));
        Assert.Equal(DirectStage.Ready,h.Current.Direct!.Stage);
    }
    [Fact]public async Task SourceChangesInvalidateSubmissionAndAcceptance() {
        var h=new Harness();await h.Submit();h.Factory.Files["src/code.cs"]="external edit";
        await Assert.ThrowsAsync<InvalidOperationException>(()=>h.Service.Verify(h.Task.TaskId,h.Revision,default));
        await h.Service.Feedback(h.Task.TaskId,h.Revision,"Reconcile edit",false,null);await h.Verify();
        h.Factory.Files["src/code.cs"]="another edit";
        await Assert.ThrowsAsync<InvalidOperationException>(()=>h.Service.Accept(h.Task.TaskId,h.Revision,Owner,default));
        Assert.NotEqual(DirectStage.Done,h.Current.Direct!.Stage);
    }
    [Fact]public async Task ConcurrentSourceMutationDuringReviewCannotComplete() {
        var h=new Harness();h.Factory.MutateOnReview=true;await h.Verify();
        Assert.Equal(DirectStage.Blocked,h.Current.Direct!.Stage);
        Assert.Equal("SOURCE_CHANGED",h.Store.FindRun(h.Current.Direct.LastRunId!)!.Failure!.Code);
    }
    [Fact]public async Task OutOfBoundaryEditIsRejectedBeforeTesting() {
        var h=new Harness();await h.Ready();await h.Service.Begin(h.Task.TaskId,h.Revision,null,default);
        h.Factory.Files["protected.cs"]="bad";
        await Assert.ThrowsAsync<InvalidOperationException>(()=>h.Service.Submit(h.Task.TaskId,h.Revision,"Changed source",default));
        Assert.Empty(h.Factory.Calls);
    }
    [Fact]public async Task EnvironmentBlockRetriesVerificationWithoutNewAgreement() {
        var h=new Harness();h.Factory.BlockReview=true;await h.Verify();h.Factory.BlockReview=false;
        await h.Service.Verify(h.Task.TaskId,h.Revision,default);
        Assert.Equal(DirectStage.AwaitingAcceptance,h.Current.Direct!.Stage);
        Assert.Single(h.Current.Direct.Agreements);Assert.Equal(2,h.Store.Runs().Count);
    }
    [Fact]public async Task OpeningReloadingAndLegacyRecoveryDoNotInterruptDirectVerification() {
        var h=new Harness();await h.Submit();h.Factory.Hold=true;
        var checking=h.Service.Verify(h.Task.TaskId,h.Revision,default);await h.Factory.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var observer=new ObserveModel(new SqliteWorkbenchStore(h.Root));observer.OnGet(h.Task.TaskId);
        Assert.Equal(DirectStage.Checking,observer.TaskItem!.Direct!.Stage);
        var legacy=new WorkbenchService(h.Store,h.Workspaces,h.Factory,new MicrosoftAgentFrameworkBackend(),new RunQueue());
        await legacy.RecoverInterruptedAsync();
        Assert.Equal(RunState.Running,h.Store.FindRun(h.Current.Direct!.LastRunId!)!.State);
        h.Factory.Release.TrySetResult();await checking;
        Assert.Equal(DirectStage.AwaitingAcceptance,h.Current.Direct!.Stage);
    }
    [Fact]public async Task TwoTasksCannotClaimSameProjectAndLegacyCannotLaunchDirectPrimary() {
        var h=new Harness();await h.Submit();
        var other=h.Service.Create("p","Another task",Risk.Standard,"another conversation",null);
        await Assert.ThrowsAsync<InvalidOperationException>(()=>h.Service.Agree(other.TaskId,0,Anchors,[new(1,"automatic","test")],Owner,default));
        var legacy=new WorkbenchService(h.Store,h.Workspaces,h.Factory,new MicrosoftAgentFrameworkBackend(),new RunQueue());
        Assert.Throws<InvalidOperationException>(()=>legacy.RequestRun(h.Task.TaskId));
    }
}
