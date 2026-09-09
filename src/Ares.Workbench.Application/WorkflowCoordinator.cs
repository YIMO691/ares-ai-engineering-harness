using System.Collections.Immutable;
using Ares.Workbench.Domain;
namespace Ares.Workbench.Application;

/// <summary>The sole application boundary that commits task/run truth. Backend and node handlers only receive immutable snapshots.</summary>
public sealed class WorkflowCoordinator(ITaskStore tasks, IRunStore runs, IEventSink events,
    IWorkflowBackend backend, IContextBuilder contexts, IReadOnlyDictionary<string,INodeHandler> handlers, Func<bool>? pauseRequested = null, object? controlSync = null)
{
    private readonly SemaphoreSlim gate=new(1,1);
    private readonly object completionSync=controlSync??new();
    private readonly Dictionary<string,(WorkflowDefinition Definition,Workspace Workspace)> bindings=[];
    public EngineeringTask Task(string id)=>tasks.Get(id);
    public WorkflowRun Run(string id)=>runs.Get(id);
    private async ValueTask Emit(WorkflowRun run,string type,string? node=null,int? attempt=null,params (string Key,string Value)[] fields)
    {
        await events.AppendAsync(new(Guid.NewGuid().ToString("N"),type,DateTimeOffset.UtcNow,
            run.TaskId,run.RunId,node,attempt,fields.ToImmutableDictionary(x=>x.Key,x=>x.Value)));
    }
    private WorkflowRun Commit(WorkflowRun run)
    {
        var next=run with { Version=run.Version+1,UpdatedAt=DateTimeOffset.UtcNow };
        runs.Update(next);return next;
    }
    public async Task<WorkflowRun> StartAsync(string taskId,WorkflowDefinition definition,Workspace workspace,CancellationToken cancellationToken=default,string? runId=null)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            definition.Validate();
            var task=tasks.Get(taskId);
            if(task.WorkspaceId!=workspace.WorkspaceId||task.Risk!=definition.Risk ||
                task.Lifecycle is TaskLifecycle.Delivered or TaskLifecycle.Cancelled or TaskLifecycle.Done ||runs.HasActiveRun(taskId))
                throw new InvalidOperationException("Task cannot start this workflow.");
            var now=DateTimeOffset.UtcNow;
            var run=new WorkflowRun(runId??Guid.NewGuid().ToString("N"),taskId,definition.WorkflowId,definition.Version,backend.BackendId,
                RunState.Created,definition.EntryNode,0,now,now,0,[],null,null);
            runs.Add(run);bindings.Add(run.RunId,(definition,workspace));
            tasks.Update(task with { Lifecycle=TaskLifecycle.Active,UpdatedAt=now });
            await Emit(run,"TaskCreated",fields: [("risk",task.Risk.ToString())]);
            run=Commit(run with { State=RunState.Running });
            await Emit(run,"WorkflowStarted",fields:[("workflow",definition.WorkflowId),("backend",backend.BackendId)]);
            return await Drive(run.RunId,cancellationToken);
        }
        finally { gate.Release(); }
    }
    public async Task<WorkflowRun> ResumeAsync(string id,WorkflowDefinition definition,Workspace workspace,CancellationToken ct=default)
    {
        await gate.WaitAsync(ct);
        try {
            var run=runs.Get(id);
            if(run.State is not (RunState.Blocked or RunState.Paused) || run.CurrentNode is null ||
                run.WorkflowId!=definition.WorkflowId || run.WorkflowVersion!=definition.Version)
                throw new InvalidOperationException("Run cannot resume this definition.");
            definition.Validate();bindings[id]=(definition,workspace);
            run=Commit(run with {State=RunState.Running,Failure=null,PendingHuman=null});
            await Emit(run,"WorkflowResumed",run.CurrentNode);
            return await Drive(id,ct);
        } finally {gate.Release();}
    }
    private bool IsPause => pauseRequested?.Invoke()==true;
    private async Task<WorkflowRun> Drive(string id,CancellationToken ct)
    {
        var (definition,_)=bindings[id];
        try
        {
            await backend.ExecuteAsync(definition,runs.Get(id).CurrentNode!,
                async(node,token)=>{
                    using var dispatchToken=CancellationTokenSource.CreateLinkedTokenSource(token,ct);
                    return await Dispatch(id,node,dispatchToken.Token);
                },type=>Emit(runs.Get(id),"BackendEvent",fields:[("type",type)]),ct);
            var after=runs.Get(id);
            if(after.State==RunState.Running) throw new InvalidOperationException("Backend stopped before a domain terminal/waiting state.");
        }
        catch(Exception ex) when (ex is OperationCanceledException || ct.IsCancellationRequested)
        {
            var current=runs.Get(id);
            if(current.State is not (RunState.Completed or RunState.Cancelled or RunState.Paused)) {
                // Cancellation can arrive between backend dispatches as well as inside a handler.
                if(current.CurrentNode is not null) {
                    var now=DateTimeOffset.UtcNow;
                    var result=new NodeResult(IsPause?NodeOutcome.Interrupted:NodeOutcome.Cancelled,"node-result/v1","Interrupted",[]);
                    current=current with {Executions=current.Executions.Add(new(current.CurrentNode,
                        current.Executions.Count(e=>e.NodeId==current.CurrentNode)+1,now,now,result))};
                }
                var run=Commit(current with {State=IsPause?RunState.Paused:RunState.Cancelled,PendingHuman=null,
                    Failure=new(IsPause?"STOPPED":"CANCELLED",IsPause?"当前步骤已中断，可继续；已有源码改动保留。":"Run 已取消，已有源码改动保留。")});
                await Emit(run,IsPause?"WorkflowPaused":"WorkflowCancelled");
            }
        }
        catch(Exception ex)
        {
            var current=runs.Get(id);
            // Observability or backend errors must never manufacture a successful delivery.
            var run=Commit(current with { State=RunState.Failed,Failure=new("RUNTIME_ERROR",ex.GetType().Name) });
            var task=tasks.Get(run.TaskId);
            if(task.Lifecycle==TaskLifecycle.Delivered)tasks.Update(task with { Lifecycle=TaskLifecycle.Active });
            await Emit(run,"WorkflowFailed",fields:[("code",run.Failure!.Code)]);
        }
        return runs.Get(id);
    }
    private async ValueTask<string?> Dispatch(string id,string nodeId,CancellationToken ct)
    {
        var run=runs.Get(id);var (definition,workspace)=bindings[id];
        if(run.State!=RunState.Running||run.CurrentNode!=nodeId||run.BackendId!=backend.BackendId)
            throw new InvalidOperationException("Backend attempted unauthorized node dispatch.");
        var node=definition.Node(nodeId);
        if(!handlers.TryGetValue(node.Handler,out var handler))throw new InvalidOperationException("Missing node handler.");
        var task=tasks.Get(run.TaskId);
        var attempt=run.Executions.Count(x=>x.NodeId==nodeId)+1;
        var start=DateTimeOffset.UtcNow;
        await Emit(run,"NodeStarted",nodeId,attempt,("kind",node.Kind.ToString()));
        var context=await contexts.BuildAsync(task,workspace,run,ct);
        NodeResult result;
        NodeResult? returned=null;
        using var timeout=new CancellationTokenSource(node.Timeout);
        using var linked=CancellationTokenSource.CreateLinkedTokenSource(ct,timeout.Token);
        try
        {
            if(node.Kind==NodeKind.Agent)await Emit(run,"AgentCalled",nodeId,attempt,("role",node.RoleId??node.Handler));
            result=await handler.ExecuteAsync(new(node,task,run,workspace,context),linked.Token);
            returned=result;
            linked.Token.ThrowIfCancellationRequested();
            result.Validate(node);
        }
        catch(OperationCanceledException)
        {
            result=ct.IsCancellationRequested?new(IsPause?NodeOutcome.Interrupted:NodeOutcome.Cancelled,"node-result/v1",IsPause?"Interrupted":"Cancelled",returned?.ArtifactRefs??[]):
                NodeResult.Fail("TIMEOUT","Node deadline exceeded.",false,node.SideEffect==SideEffect.ReadOnly?EffectStatus.NotStarted:EffectStatus.Unknown);
        }
        catch(Exception ex) { result=NodeResult.Fail("INVALID_NODE_RESULT_OR_EXECUTION",ex.GetType().Name,false,node.SideEffect==SideEffect.ReadOnly?EffectStatus.NotStarted:EffectStatus.Unknown); }
        run=Commit(run with { Executions=run.Executions.Add(new(nodeId,attempt,start,DateTimeOffset.UtcNow,result)) });
        await Emit(run,"NodeCompleted",nodeId,attempt,("outcome",result.Outcome.ToString()));
        if(nodeId=="review")await Emit(run,"ReviewFinished",nodeId,attempt,("outcome",result.Outcome.ToString()),("finding",result.Output));
        if(nodeId is "test" or "independent-verify")await Emit(run,"BuildFinished",nodeId,attempt,("outcome",result.Outcome.ToString()));
        if(result.Outcome is not (NodeOutcome.Cancelled or NodeOutcome.Interrupted))ct.ThrowIfCancellationRequested();
        if(result.Outcome==NodeOutcome.Waiting)
        {
            var request=result.HumanRequest! with { RunVersion=run.Version+1 };
            run=Commit(run with { State=RunState.Waiting,PendingHuman=request });
            await Emit(run,"ApprovalRequired",nodeId,attempt,("request_id",request.RequestId),("scope",request.Scope));
            return null;
        }
        if(result.Outcome is NodeOutcome.Failed or NodeOutcome.Cancelled or NodeOutcome.Interrupted)
        {
            // Transient retries are opt-in and only safe before any side effect occurred.
            if(result.Outcome==NodeOutcome.Failed && result.Failure is {Retryable:true,Effect:EffectStatus.NotStarted} &&
                attempt<=node.Retry.MaxRetries)
            {
                await Emit(run,"NodeRetryScheduled",nodeId,attempt);return nodeId;
            }
            var state=result.Outcome==NodeOutcome.Interrupted?RunState.Paused:result.Outcome==NodeOutcome.Cancelled?RunState.Cancelled:
                result.Failure?.Effect==EffectStatus.Unknown?RunState.Blocked:RunState.Failed;
            run=Commit(run with { State=state,Failure=result.Failure??(state==RunState.Paused?new("STOPPED","当前步骤已中断，可继续；已有源码改动保留。"):null) });
            await Emit(run,state==RunState.Paused?"WorkflowPaused":state==RunState.Blocked?"WorkflowBlocked":state==RunState.Cancelled?"WorkflowCancelled":"WorkflowFailed",nodeId,attempt);
            return null;
        }
        if(result.Outcome==NodeOutcome.ReworkRequired)
        {
            if(!definition.Routes.Any(r=>r.From==nodeId&&r.Outcome==NodeOutcome.ReworkRequired))
                throw new InvalidOperationException("Rework not allowed at this node.");
            if(run.ReworkCount>=definition.ReworkPolicy.MaxReworks)
            {
                run=Commit(run with { State=RunState.Blocked,Failure=new("REWORK_BUDGET","Explicit Owner decision required.") });
                await Emit(run,"ReworkBudgetExhausted",nodeId,attempt);return null;
            }
            run=Commit(run with { ReworkCount=run.ReworkCount+1,CurrentNode=definition.ReworkPolicy.TargetNode });
            await Emit(run,"ReworkRequested",nodeId,attempt,("count",run.ReworkCount.ToString()),("target",run.CurrentNode!));
            return run.CurrentNode;
        }
        return await Advance(run,definition,nodeId,ct);
    }
    private async ValueTask<string?> Advance(WorkflowRun run,WorkflowDefinition definition,string nodeId,CancellationToken ct)
    {
        var next=definition.Next(nodeId,NodeOutcome.Succeeded);
        if(next=="$complete")
        {
            foreach(var required in definition.CompletionConditions)
                if(run.Executions.LastOrDefault(x=>x.NodeId==required)?.Result.Outcome!=NodeOutcome.Succeeded)
                    throw new InvalidOperationException("Completion condition missing: "+required);
            lock(completionSync) {
                ct.ThrowIfCancellationRequested();
                run=Commit(run with { State=RunState.Completed,CurrentNode=null,PendingHuman=null });
                var task=tasks.Get(run.TaskId);
                tasks.Update(task with { Lifecycle=task.Fusion is null && task.Direct is null?TaskLifecycle.Delivered:TaskLifecycle.AwaitingAcceptance,UpdatedAt=DateTimeOffset.UtcNow });
            }
            await Emit(run,"WorkflowCompleted");
            return null;
        }
        ct.ThrowIfCancellationRequested();
        Commit(run with { CurrentNode=next });return next;
    }
    public async Task<WorkflowRun> SubmitHumanAsync(string id,HumanDecision decision,CancellationToken ct=default)
    {
        await gate.WaitAsync(ct);
        try
        {
            var run=runs.Get(id);var request=run.PendingHuman;
            if(run.State!=RunState.Waiting||request is null||request.RequestId!=decision.RequestId||
               request.Scope!=decision.Scope||request.RunVersion!=decision.RunVersion||string.IsNullOrWhiteSpace(decision.Actor))
                throw new InvalidOperationException("Human decision does not match pending request.");
            var old=run.Executions[^1];
            var result=decision.Approved?NodeResult.Success(decision.Reason):NodeResult.Fail("HUMAN_REJECTED",decision.Reason);
            run=Commit(run with { Executions=run.Executions.SetItem(run.Executions.Length-1,old with {Result=result}),
                PendingHuman=null,State=decision.Approved?RunState.Running:RunState.Failed,Failure=result.Failure });
            await Emit(run,"ApprovalRecorded",run.CurrentNode,old.Attempt,("actor",decision.Actor),("approved",decision.Approved.ToString()));
            if(!decision.Approved){await Emit(run,"WorkflowFailed");return run;}
            var next=await Advance(run,bindings[id].Definition,run.CurrentNode!,ct);
            return next is null?runs.Get(id):await Drive(id,ct);
        }
        finally {gate.Release();}
    }
}
