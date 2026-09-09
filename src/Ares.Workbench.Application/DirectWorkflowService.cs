using System.Collections.Immutable;
using System.Text.Json;
using Ares.Workbench.Domain;
namespace Ares.Workbench.Application;

/// <summary>Business gates for an external Primary; never starts or resumes that Primary.</summary>
/// <remarks>The CLI holds an exclusive writer lease. Web observer performs no mutations.</remarks>
public sealed class DirectWorkflowService(IWorkbenchStore store, IProjectWorkspace workspaces,
    IRunHandlerFactory handlers, IDirectEvidence evidence, IWorkflowBackend backend)
{
    private sealed class Handler(Func<NodeInvocation,CancellationToken,ValueTask<NodeResult>> call):INodeHandler {
        public ValueTask<NodeResult> ExecuteAsync(NodeInvocation i,CancellationToken ct)=>call(i,ct);
    }
    public static string ProjectFingerprint(ProjectProfile p)=>Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
        System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new {p.RepoRoot,p.AllowedPaths,p.InstructionFiles,p.BuildCommand,p.TestCommand}))));
    private EngineeringTask Editable(string id,long revision,params DirectStage[] allowed) {
        var t=store.Task(id);
        if(t.Direct is not {} d || d.Revision!=revision)throw new InvalidOperationException("Task revision changed; read status before retrying.");
        if(!allowed.Contains(d.Stage))throw new InvalidOperationException("Operation unavailable at "+d.Stage);
        return t;
    }
    private EngineeringTask Save(EngineeringTask t,DirectTask d,TaskLifecycle lifecycle) {
        var next=t with {Direct=d with {Revision=d.Revision+1},Lifecycle=lifecycle,UpdatedAt=DateTimeOffset.UtcNow};
        ((ITaskStore)store).Update(next);return next;
    }
    private ValueTask Audit(EngineeringTask t,string kind,string detail)=>store.AppendAsync(new(Guid.NewGuid().ToString("N"),kind,
        DateTimeOffset.UtcNow,t.TaskId,t.TaskId,null,null,new Dictionary<string,string>{{"detail",detail},{"source","external-primary"}}.ToImmutableDictionary()));
    private void NoOtherWriter(EngineeringTask t) {
        if(store.Tasks().Any(x=>x.TaskId!=t.TaskId&&x.WorkspaceId==t.WorkspaceId&&x.Direct?.Stage is DirectStage.Implementing or DirectStage.Submitted or DirectStage.Checking or DirectStage.Rework or DirectStage.Blocked or DirectStage.AwaitingAcceptance)
            ||store.Runs().Any(r=>r.TaskId!=t.TaskId&&store.Task(r.TaskId).WorkspaceId==t.WorkspaceId&&r.State is RunState.Created or RunState.Running or RunState.Waiting))
            throw new InvalidOperationException("Another task owns this project. Finish it or explicitly release its work before starting.");
    }
    private ProjectProfile FrozenProject(EngineeringTask t) {
        var p=workspaces.Validate(store.Project(t.WorkspaceId));
        if(t.Direct?.Agreement is not {} a||a.ProjectFingerprint!=ProjectFingerprint(p))
            throw new InvalidOperationException("Project policy or verification commands changed. Reopen the agreement and confirm the difference.");
        return p;
    }
    public EngineeringTask Create(string projectId,string title,Risk risk,string primaryLabel,string? sessionRef) {
        var p=workspaces.Validate(store.Project(projectId));
        if(string.IsNullOrWhiteSpace(title)||string.IsNullOrWhiteSpace(primaryLabel)||!Enum.IsDefined(risk))
            throw new ArgumentException("Title, external Primary identity and workflow are required.");
        var now=DateTimeOffset.UtcNow;
        var t=new EngineeringTask(Guid.NewGuid().ToString("N"),title.Trim(),"",[],[],risk,p.ProjectId,TaskLifecycle.Discussing,now,now) {
            Direct=new(primaryLabel,sessionRef,DirectStage.Discussing,0,[])
        };
        ((ITaskStore)store).Add(t);return t;
    }
    public async Task<EngineeringTask> Agree(string id,long revision,ReadyAnchors anchors,ImmutableArray<AcceptanceCheck> checks,OwnerEvidence owner,CancellationToken ct) {
        var t=Editable(id,revision,DirectStage.Discussing);anchors.Validate();owner.Validate();NoOtherWriter(t);
        if(checks.IsDefault||checks.Length!=anchors.Acceptance.Length||checks.Select(c=>c.Criterion).Distinct().Count()!=checks.Length||
            checks.Any(c=>c.Criterion<1||c.Criterion>anchors.Acceptance.Length||c.Kind is not ("automatic" or "manual")||
                string.IsNullOrWhiteSpace(c.Method)||c.Kind=="automatic"&&c.Method is not ("build" or "test")))
            throw new ArgumentException("Map every acceptance criterion once: automatic build/test, or manual with an explicit method.");
        var p=workspaces.Validate(store.Project(t.WorkspaceId));var w=workspaces.Create(p,t.TaskId);
        var snapshot=await evidence.SnapshotAsync(p,w,ct);
        var a=new DirectAgreement(t.Direct!.Agreements.Length+1,anchors,checks,ProjectFingerprint(p),snapshot.WorkspaceStamp,snapshot.Files,owner,DateTimeOffset.UtcNow,p.BuildCommand,p.TestCommand);
        t=Save(t with {Goal=anchors.Goal,Acceptance=anchors.Acceptance,Constraints=[anchors.NonGoals,anchors.Boundary]},
            t.Direct with {Agreements=t.Direct!.Agreements.Add(a),Stage=DirectStage.Ready,LastReport=null,SubmittedStamp=null,Note=null,RiskApproval=null},TaskLifecycle.Ready);
        await Audit(t,"DirectAgreementFrozen",JsonSerializer.Serialize(a));return t;
    }
    public async Task<EngineeringTask> Begin(string id,long revision,OwnerEvidence? riskApproval,CancellationToken ct) {
        var t=Editable(id,revision,DirectStage.Ready,DirectStage.Rework,DirectStage.Blocked);NoOtherWriter(t);var p=FrozenProject(t);
        var w=workspaces.Create(p,t.TaskId);var snapshot=await evidence.SnapshotAsync(p,w,ct);
        if(snapshot.WorkspaceStamp!=t.Direct!.Agreement!.WorkspaceStamp)throw new InvalidOperationException("Git reference or instructions changed; reconcile and reopen the agreement.");
        if(t.Direct.Stage==DirectStage.Ready && snapshot.Fingerprint!=new SourceSnapshot(t.Direct.Agreement.WorkspaceStamp,t.Direct.Agreement.BaselineFiles).Fingerprint)
            throw new InvalidOperationException("Source changed after the agreement; reconcile it and freeze a new version before implementation.");
        if(t.Risk==Risk.Critical && t.Direct!.RiskApproval is null){(riskApproval??throw new ArgumentException("CRITICAL needs explicit approval of the frozen write boundary before implementation.")).Validate();}
        t=Save(t,t.Direct with {Stage=DirectStage.Implementing,LastReport=null,SubmittedStamp=null,
            RiskApproval=riskApproval??t.Direct!.RiskApproval,Note="External Primary owns implementation; native tool progress is not collected."},TaskLifecycle.Active);
        await Audit(t,"ExternalPrimaryStarted",JsonSerializer.Serialize(new {t.Direct!.PrimaryLabel,t.Direct!.NativeSessionRef,t.Direct!.RiskApproval}));return t;
    }
    public async Task<EngineeringTask> Submit(string id,long revision,string report,CancellationToken ct) {
        var t=Editable(id,revision,DirectStage.Implementing);
        if(string.IsNullOrWhiteSpace(report))throw new ArgumentException("An implementation report is required.");
        var p=FrozenProject(t);var w=workspaces.Create(p,t.TaskId);var current=await evidence.SnapshotAsync(p,w,ct);
        ValidateChanges(t,p,w,current);
        t=Save(t,t.Direct! with {Stage=DirectStage.Submitted,LastReport=report,SubmittedStamp=current.Fingerprint,Note=null},TaskLifecycle.Active);
        await Audit(t,"ExternalPrimarySubmitted",report);return t;
    }
    private void ValidateChanges(EngineeringTask t,ProjectProfile p,Workspace w,SourceSnapshot current) {
        var a=t.Direct!.Agreement!;
        if(current.WorkspaceStamp!=a.WorkspaceStamp)throw new InvalidOperationException("Git reference or instruction files changed after agreement.");
        var changed=a.BaselineFiles.Keys.Union(current.Files.Keys,StringComparer.OrdinalIgnoreCase)
            .Where(k=>a.BaselineFiles.GetValueOrDefault(k)!=current.Files.GetValueOrDefault(k)).ToArray();
        var denied=changed.Where(k=>!evidence.Writable(p,w,k)).ToArray();
        if(denied.Length>0)throw new InvalidOperationException("Changes outside agreed write policy: "+string.Join(", ",denied));
    }
    public static WorkflowDefinition Definition(Risk risk) {
        var sequence=risk==Risk.Fast?new[]{"submitted","test","deliver"}:new[]{"submitted","test","review","deliver"};
        var nodes=sequence.Append("primary-rework").Select(id=>new NodeDefinition(id,id=="review"?NodeKind.Agent:NodeKind.Deterministic,
            "context/v1","node-result/v1",SideEffect.ReadOnly,TimeSpan.FromMinutes(id=="review"?10:3),new(0),id)).ToImmutableArray();
        var routes=sequence.Select((id,n)=>new Route(id,NodeOutcome.Succeeded,n+1<sequence.Length?sequence[n+1]:"$complete")).ToList();
        routes.Add(new("test",NodeOutcome.ReworkRequired,"primary-rework"));
        if(risk!=Risk.Fast)routes.Add(new("review",NodeOutcome.ReworkRequired,"primary-rework"));
        var d=new WorkflowDefinition("DIRECT-"+risk.ToString().ToUpperInvariant()+"-v1",1,risk,"submitted",nodes,[..routes],[..sequence],new(1,"primary-rework"));
        d.Validate();return d;
    }
    public async Task<EngineeringTask> Verify(string id,long revision,CancellationToken ct) {
        var t=Editable(id,revision,DirectStage.Submitted,DirectStage.Blocked);NoOtherWriter(t);
        if(t.Direct!.SubmittedStamp is null)throw new InvalidOperationException("Submit the current implementation first.");
        var p=FrozenProject(t);var runId=Guid.NewGuid().ToString("N");var w=workspaces.Create(p,runId);
        var snapshot=await evidence.SnapshotAsync(p,w,ct);ValidateChanges(t,p,w,snapshot);
        if(snapshot.Fingerprint!=t.Direct!.SubmittedStamp)throw new InvalidOperationException("Source changed after submission; begin and submit a fresh implementation.");
        store.SaveTicket(new(runId,t.TaskId,p,"Running",DateTimeOffset.UtcNow));
        store.SaveCheckpoint(new(runId,RunState.Created,"submitted",null,null,null,[],0,null,null,DateTimeOffset.UtcNow,
            RunCheckpoint.Fingerprint(t),p.RepoRoot,snapshot.Fingerprint));
        t=Save(t,t.Direct with {Stage=DirectStage.Checking,LastRunId=runId,Note=null},TaskLifecycle.Active);
        await Audit(t,"DirectVerificationStarted",runId);
        var parts=handlers.Create(p,w);var map=parts.Handlers.ToDictionary(x=>x.Key,x=>x.Value);
        map["submitted"]=new Handler(async(i,token)=>{
            var diff=await evidence.DiffAsync(p,w,token);
            var path=Path.Combine(w.ArtifactRoot,"submitted.diff.patch");
            await File.WriteAllTextAsync(path,diff,token);
            return NodeResult.Success("External Primary report (reported, not independently verified):\n"+t.Direct!.LastReport,path);
        });
        map["primary-rework"]=new Handler((i,token)=>ValueTask.FromResult(NodeResult.Fail("PRIMARY_REWORK_REQUIRED",
            "Return the findings to the existing external Primary. Repair, submit and verify again; the agreement remains frozen.")));
        map["deliver"]=new Handler(async(i,token)=>{
            var now=await evidence.SnapshotAsync(p,w,token);
            if(now.Fingerprint!=snapshot.Fingerprint)return NodeResult.Fail("SOURCE_CHANGED","Source changed during verification; all checks must be repeated.");
            var text="Verification complete; awaiting Owner acceptance.\n\n"+
                string.Join("\n",t.Direct!.Agreement!.Checks.Select(c=>$"- {c.Criterion}. {t.Acceptance[c.Criterion-1]} — {c.Kind}: {c.Method}"))+
                "\n\nAutomatic mappings identify checks run; they do not by themselves prove every criterion. Manual criteria require Owner confirmation.";
            var path=Path.Combine(w.ArtifactRoot,"delivery.md");await File.WriteAllTextAsync(path,text,token);
            return NodeResult.Success(text,path);
        });
        try {
            var coordinator=new WorkflowCoordinator(store,store,store,backend,parts.Contexts,map);
            var run=await coordinator.StartAsync(t.TaskId,Definition(t.Risk),w,ct,runId);
            t=store.Task(id);
            var stage=run.State==RunState.Completed?DirectStage.AwaitingAcceptance:
                run.Failure?.Code=="PRIMARY_REWORK_REQUIRED"?DirectStage.Rework:DirectStage.Blocked;
            t=Save(t,t.Direct! with {Stage=stage,Note=run.Failure?.Message},
                stage==DirectStage.AwaitingAcceptance?TaskLifecycle.AwaitingAcceptance:TaskLifecycle.Active);
            store.SaveTicket(store.Ticket(runId) with {State="Finished",Error=run.Failure?.Message});
            await Audit(t,"DirectVerificationFinished",runId+": "+stage);return t;
        } catch(Exception ex) {
            t=store.Task(id);
            var run=store.FindRun(runId);
            if(run is not null&&run.State!=RunState.Completed)
                ((IRunStore)store).Update(run with {State=RunState.Blocked,Failure=new("DIRECT_INTERRUPTED",ex.Message),Version=run.Version+1,UpdatedAt=DateTimeOffset.UtcNow});
            t=Save(t,t.Direct! with {Stage=DirectStage.Blocked,Note=ex.Message},TaskLifecycle.Active);
            store.SaveTicket(store.Ticket(runId) with {State="Finished",Error=ex.Message});
            await Audit(t,"DirectVerificationInterrupted",ex.Message);throw;
        }
    }
    public async Task<EngineeringTask> Accept(string id,long revision,OwnerEvidence owner,CancellationToken ct) {
        var t=Editable(id,revision,DirectStage.AwaitingAcceptance);owner.Validate();var p=FrozenProject(t);
        var run=store.FindRun(t.Direct!.LastRunId!);
        if(run?.State!=RunState.Completed)throw new InvalidOperationException("Latest verification is not complete.");
        var current=await evidence.SnapshotAsync(p,workspaces.Create(p,t.TaskId),ct);
        if(current.Fingerprint!=t.Direct!.SubmittedStamp)throw new InvalidOperationException("Source changed after verification; repeat checks before accepting.");
        t=Save(t,t.Direct with {Stage=DirectStage.Done,Note=owner.Quote,AcceptanceDecision=owner},TaskLifecycle.Done);
        store.SaveApproval(new(Guid.NewGuid().ToString("N"),run.RunId,"Accepted",true,
            JsonSerializer.Serialize(new {owner,recorded_by=t.Direct!.PrimaryLabel}),DateTimeOffset.UtcNow,DateTimeOffset.UtcNow));
        await Audit(t,"DirectOwnerAccepted",JsonSerializer.Serialize(owner));return t;
    }
    public async Task<EngineeringTask> Feedback(string id,long revision,string message,bool scopeChange,OwnerEvidence? owner) {
        var t=Editable(id,revision,DirectStage.Ready,DirectStage.Implementing,DirectStage.Submitted,DirectStage.Rework,DirectStage.Blocked,DirectStage.AwaitingAcceptance);
        if(string.IsNullOrWhiteSpace(message))throw new ArgumentException("Explain the defect or scope change.");
        if(scopeChange)(owner??throw new ArgumentException("Scope changes require the Owner instruction.")).Validate();
        t=Save(t,t.Direct! with {Stage=scopeChange?DirectStage.Discussing:DirectStage.Rework,Note=message,SubmittedStamp=null},
            scopeChange?TaskLifecycle.Discussing:TaskLifecycle.Active);
        await Audit(t,scopeChange?"DirectScopeReopened":"DirectReworkRequested",JsonSerializer.Serialize(new {message,owner}));return t;
    }
    public async Task<EngineeringTask> Recover(string id,long revision,OwnerEvidence acknowledgement) {
        var t=Editable(id,revision,DirectStage.Checking);acknowledgement.Validate();
        // Caller must hold writer lease and reconcile any surviving native children. No automatic replay.
        if(t.Direct!.LastRunId is {} runId) {
            var run=store.FindRun(runId);
            if(run is not null&&run.State!=RunState.Completed)((IRunStore)store).Update(run with {State=RunState.Blocked,
                Failure=new("INTERRUPTED","Previous command interrupted; revalidation required."),Version=run.Version+1,UpdatedAt=DateTimeOffset.UtcNow});
            store.SaveTicket(store.Ticket(runId) with {State="Interrupted"});
        }
        t=Save(t,t.Direct with {Stage=DirectStage.Blocked,Note="Interruption acknowledged; retry verification after checking current source."},TaskLifecycle.Active);
        await Audit(t,"DirectRecoveryAcknowledged",JsonSerializer.Serialize(acknowledgement));return t;
    }
}
