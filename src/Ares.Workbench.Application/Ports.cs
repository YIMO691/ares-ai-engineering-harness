using Ares.Workbench.Domain;
namespace Ares.Workbench.Application;

public interface ITaskStore { EngineeringTask Get(string id); void Add(EngineeringTask task); void Update(EngineeringTask task); }
public interface IRunStore { WorkflowRun Get(string id); void Add(WorkflowRun run); void Update(WorkflowRun run); bool HasActiveRun(string taskId); }
public interface IEventSink { ValueTask AppendAsync(WorkbenchEvent value, CancellationToken cancellationToken = default); }
public sealed record NodeInvocation(NodeDefinition Node, EngineeringTask Task, WorkflowRun Run, Workspace Workspace, ContextPackage Context);
public interface INodeHandler { ValueTask<NodeResult> ExecuteAsync(NodeInvocation invocation, CancellationToken cancellationToken); }
public interface IContextBuilder { ValueTask<ContextPackage> BuildAsync(EngineeringTask task, Workspace workspace, WorkflowRun run, CancellationToken cancellationToken); }
public interface IWorkflowBackend
{
    string BackendId { get; }
    Task ExecuteAsync(WorkflowDefinition definition, string entry,
        Func<string,CancellationToken,ValueTask<string?>> dispatch,
        Func<string,ValueTask> observe, CancellationToken cancellationToken);
}
public interface ICodexExecutor { ValueTask<NodeResult> ExecuteAsync(NodeInvocation invocation, CancellationToken cancellationToken); }
public interface IAgentInvoker { ValueTask<NodeResult> InvokeAsync(NodeInvocation invocation, CancellationToken cancellationToken); }
public sealed class CodexNodeHandler(ICodexExecutor executor) : INodeHandler
{
    public ValueTask<NodeResult> ExecuteAsync(NodeInvocation invocation, CancellationToken cancellationToken) => executor.ExecuteAsync(invocation,cancellationToken);
}
public sealed class AgentNodeHandler(IAgentInvoker invoker) : INodeHandler
{
    public ValueTask<NodeResult> ExecuteAsync(NodeInvocation invocation, CancellationToken cancellationToken) => invoker.InvokeAsync(invocation,cancellationToken);
}
public sealed class HumanNodeHandler : INodeHandler
{
    public ValueTask<NodeResult> ExecuteAsync(NodeInvocation i, CancellationToken cancellationToken) =>
        ValueTask.FromResult(new NodeResult(NodeOutcome.Waiting,"node-result/v1","Human decision required.",[],
            HumanRequest:new(Guid.NewGuid().ToString("N"),$"{i.Task.TaskId}:{i.Run.RunId}:{i.Run.ReworkCount}",i.Run.Version,"Approve reviewed task artifacts.")));
}
