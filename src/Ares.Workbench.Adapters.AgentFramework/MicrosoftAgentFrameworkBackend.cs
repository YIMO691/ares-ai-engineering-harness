using Ares.Workbench.Application;
using Ares.Workbench.Domain;
using Microsoft.Agents.AI.Workflows;
namespace Ares.Workbench.Adapters.AgentFramework;

public sealed class MicrosoftAgentFrameworkBackend : IWorkflowBackend
{
    public string BackendId=>"microsoft-af/1.20.0";
    private sealed record Signal(string? Target);
    private sealed class NodeExecutor(string id,Func<string,CancellationToken,ValueTask<string?>> dispatch)
        : Executor<Signal,Signal>(id)
    {
        public override async ValueTask<Signal> HandleAsync(Signal message,IWorkflowContext context,CancellationToken cancellationToken=default)
        {
            if(message.Target!=Id)throw new InvalidOperationException("AF dispatched a different target.");
            return new(await dispatch(Id,cancellationToken));
        }
    }
    public async Task ExecuteAsync(WorkflowDefinition definition,string entry,
        Func<string,CancellationToken,ValueTask<string?>> dispatch,Func<string,ValueTask> observe,CancellationToken cancellationToken)
    {
        definition.Validate();
        // Human continuation starts a fresh ephemeral AF graph over the reachable remainder.
        // The Ares WorkflowRun and definition version stay unchanged in the in-memory store.
        var reachable=new HashSet<string>{entry};var queue=new Queue<string>();queue.Enqueue(entry);
        while(queue.TryDequeue(out var from))
            foreach(var route in definition.Routes.Where(r=>r.From==from&&r.Target!="$complete"))
                if(reachable.Add(route.Target))queue.Enqueue(route.Target);
        var nodes=definition.Nodes.Where(n=>reachable.Contains(n.NodeId)).ToDictionary(n=>n.NodeId,n=>new NodeExecutor(n.NodeId,dispatch));
        var builder=new WorkflowBuilder(nodes[entry]).WithName(definition.WorkflowId);
        foreach(var route in definition.Routes.Where(r=>r.Target!="$complete"&&reachable.Contains(r.From)).Select(r=>(r.From,r.Target)).Distinct())
            builder.AddEdge<Signal>(nodes[route.From],nodes[route.Target],message=>message?.Target==route.Target);
        foreach(var node in definition.Nodes.Where(n=>n.Retry.MaxRetries>0&&reachable.Contains(n.NodeId)))
            builder.AddEdge<Signal>(nodes[node.NodeId],nodes[node.NodeId],message=>message?.Target==node.NodeId);
        builder.WithOutputFrom(nodes.Values.Select(n=>(ExecutorBinding)n).ToArray());
        var workflow=builder.Build();
        await using var run=await InProcessExecution.RunStreamingAsync(workflow,new Signal(entry),cancellationToken:cancellationToken);
        await foreach(var ev in run.WatchStreamAsync(cancellationToken))
            await observe(ev.GetType().Name);
    }
}
