using System.Collections.Immutable;
namespace Ares.Workbench.Domain;

public enum Risk { Fast, Standard, Critical }
public enum TaskLifecycle { Draft, Active, Delivered, Cancelled, Discussing, Ready, AwaitingAcceptance, Done }
public enum RunState { Created, Running, Waiting, Blocked, Completed, Failed, Cancelled, Paused }
public enum NodeKind { Deterministic, Agent, Codex, Human }
public enum NodeOutcome { Succeeded, Failed, ReworkRequired, Waiting, Cancelled, Interrupted }
public enum SideEffect { ReadOnly, WorkspaceWrite, External }
public enum EffectStatus { NotStarted, Completed, Unknown }

public sealed record EngineeringTask(string TaskId, string Title, string Goal,
    ImmutableArray<string> Acceptance, ImmutableArray<string> Constraints, Risk Risk,
    string WorkspaceId, TaskLifecycle Lifecycle, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt)
{
    public FusionTask? Fusion { get; init; }
    public DirectTask? Direct { get; init; }
}
public sealed record RetryPolicy(int MaxRetries = 0);
public sealed record ReworkPolicy(int MaxReworks, string TargetNode);
public sealed record NodeDefinition(string NodeId, NodeKind Kind, string InputContract,
    string ResultContract, SideEffect SideEffect, TimeSpan Timeout, RetryPolicy Retry,
    string Handler, string? RoleId = null);
public sealed record Route(string From, NodeOutcome Outcome, string Target);
public sealed record WorkflowDefinition(string WorkflowId, int Version, Risk Risk, string EntryNode,
    ImmutableArray<NodeDefinition> Nodes, ImmutableArray<Route> Routes,
    ImmutableArray<string> CompletionConditions, ReworkPolicy ReworkPolicy)
{
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(WorkflowId) || Version < 1 || Nodes.IsDefaultOrEmpty ||
            Nodes.Select(n => n.NodeId).Distinct().Count() != Nodes.Length ||
            !Nodes.Any(n => n.NodeId == EntryNode) || ReworkPolicy.MaxReworks < 0 ||
            !Nodes.Any(n => n.NodeId == ReworkPolicy.TargetNode) || CompletionConditions.IsDefaultOrEmpty)
            throw new ArgumentException("Invalid workflow definition.");
        foreach (var n in Nodes)
            if (n.Timeout <= TimeSpan.Zero || n.Retry.MaxRetries < 0 ||
                string.IsNullOrWhiteSpace(n.InputContract) || string.IsNullOrWhiteSpace(n.ResultContract) ||
                (n.Kind == NodeKind.Human && n.Retry.MaxRetries != 0))
                throw new ArgumentException("Invalid node contract.");
        foreach (var r in Routes)
            if (!Nodes.Any(n => n.NodeId == r.From) || (r.Target != "$complete" && !Nodes.Any(n => n.NodeId == r.Target)))
                throw new ArgumentException("Invalid route.");
        if (Routes.GroupBy(r => (r.From, r.Outcome)).Any(g => g.Count() != 1) ||
            CompletionConditions.Any(id => !Nodes.Any(n => n.NodeId == id)))
            throw new ArgumentException("Ambiguous routes or completion conditions.");
    }
    public NodeDefinition Node(string id) => Nodes.Single(n => n.NodeId == id);
    public string Next(string id, NodeOutcome outcome) =>
        Routes.SingleOrDefault(r => r.From == id && r.Outcome == outcome)?.Target ??
        throw new InvalidOperationException("No route for outcome.");
}
public sealed record Failure(string Code, string Message, bool Retryable = false, EffectStatus Effect = EffectStatus.NotStarted);
public sealed record HumanRequest(string RequestId, string Scope, long RunVersion, string Reason);
public sealed record HumanDecision(string RequestId, string Scope, long RunVersion, string Actor, bool Approved, string Reason);
public sealed record NodeResult(NodeOutcome Outcome, string Contract, string Output,
    ImmutableArray<string> ArtifactRefs, Failure? Failure = null, HumanRequest? HumanRequest = null)
{
    public static NodeResult Success(string output = "", params string[] artifacts) =>
        new(NodeOutcome.Succeeded, "node-result/v1", output, [.. artifacts]);
    public static NodeResult Fail(string code, string message, bool retryable = false, EffectStatus effect = EffectStatus.NotStarted) =>
        new(NodeOutcome.Failed, "node-result/v1", "", [], new(code, message, retryable, effect));
    public static NodeResult Rework(string finding, params string[] artifacts) =>
        new(NodeOutcome.ReworkRequired, "node-result/v1", finding, [.. artifacts]);
    public void Validate(NodeDefinition node)
    {
        if (!Enum.IsDefined(Outcome) || Contract != node.ResultContract || Output is null || ArtifactRefs.IsDefault ||
            (Outcome == NodeOutcome.Failed && Failure is null) ||
            (Outcome == NodeOutcome.Waiting && (node.Kind != NodeKind.Human || HumanRequest is null)) ||
            (Outcome != NodeOutcome.Waiting && HumanRequest is not null) ||
            (Outcome == NodeOutcome.ReworkRequired && string.IsNullOrWhiteSpace(Output)))
            throw new InvalidOperationException("Node result violates contract.");
    }
}
public sealed record NodeExecution(string NodeId, int Attempt, DateTimeOffset StartedAt, DateTimeOffset FinishedAt, NodeResult Result);
public sealed record WorkflowRun(string RunId, string TaskId, string WorkflowId, int WorkflowVersion, string BackendId,
    RunState State, string? CurrentNode, int ReworkCount, DateTimeOffset StartedAt, DateTimeOffset UpdatedAt,
    long Version, ImmutableArray<NodeExecution> Executions, HumanRequest? PendingHuman, Failure? Failure);
public sealed record Role(string RoleId, string Purpose, string InstructionRef, ImmutableArray<string> CapabilityIds);
public sealed record Capability(string CapabilityId, ImmutableArray<string> ToolIds,
    ImmutableArray<string> ContextProviderIds, string PermissionPolicy);
public sealed record ToolDefinition(string ToolId, string InputSchema, string OutputSchema, SideEffect SideEffect,
    TimeSpan Timeout, string Permission);
public sealed record Workspace(string WorkspaceId, string RepoRoot, string GitRef, ImmutableArray<string> AllowedPaths,
    string ArtifactRoot, string IsolationMode);
public sealed record ContextPackage(EngineeringTask Task, Workspace Workspace,
    ImmutableDictionary<string, string> SelectedFiles, ImmutableArray<string> Facts,
    ImmutableArray<NodeExecution> PreviousResults, ImmutableArray<string> Constraints, ImmutableArray<string> References);
public sealed record WorkbenchEvent(string EventId, string EventType, DateTimeOffset OccurredAt,
    string TaskId, string RunId, string? NodeId, int? Attempt, ImmutableDictionary<string, string> Payload);
