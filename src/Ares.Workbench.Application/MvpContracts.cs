using System.Collections.Immutable;
using Ares.Workbench.Domain;
namespace Ares.Workbench.Application;

public sealed record ProjectProfile(string ProjectId, string Name, string RepoRoot, Risk DefaultRisk,
    string BuildCommand, string TestCommand, ImmutableArray<string> AllowedPaths,
    ImmutableArray<string> InstructionFiles, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
public sealed record RunTicket(string RunId, string TaskId, ProjectProfile Project, string State,
    DateTimeOffset CreatedAt, string? Error = null);
public sealed record ArtifactEntry(string ArtifactId, string RunId, string Kind, string RelativePath,
    string DisplayName, DateTimeOffset CreatedAt);
public sealed record ApprovalEntry(string ApprovalId, string RunId, string Status, bool? Decision,
    string Comment, DateTimeOffset CreatedAt, DateTimeOffset? DecidedAt);
public sealed record QueueItem(string RunId, HumanDecision? Decision = null, bool Resume = false);
public interface IWorkbenchQueue { void Enqueue(QueueItem item); }
public interface IWorkbenchStore : ITaskStore, IRunStore, IEventSink
{
    IReadOnlyList<ProjectProfile> Projects();
    ProjectProfile Project(string id);
    void SaveProject(ProjectProfile project);
    IReadOnlyList<EngineeringTask> Tasks();
    EngineeringTask Task(string id);
    IReadOnlyList<WorkflowRun> Runs();
    WorkflowRun? FindRun(string id);
    IReadOnlyList<WorkbenchEvent> Events(string runId);
    IReadOnlyList<ArtifactEntry> Artifacts(string runId);
    void SaveArtifact(ArtifactEntry entry);
    IReadOnlyList<ApprovalEntry> Approvals(string runId);
    void SaveApproval(ApprovalEntry entry);
    IReadOnlyList<RunTicket> Tickets();
    RunTicket Ticket(string id);
    void SaveTicket(RunTicket ticket);
    RunCheckpoint? Checkpoint(string runId);
    void SaveCheckpoint(RunCheckpoint checkpoint);
    void UpdateCheckpoint(string runId, Func<RunCheckpoint,RunCheckpoint> update);
}
public interface IProjectWorkspace
{
    ProjectProfile Validate(ProjectProfile profile);
    Workspace Create(ProjectProfile profile, string runId);
}
public sealed record RunHandlers(IContextBuilder Contexts, IReadOnlyDictionary<string, INodeHandler> Handlers);
public interface IRunHandlerFactory {
    RunHandlers Create(ProjectProfile project, Workspace workspace);
    ValueTask<string> WorkspaceStampAsync(ProjectProfile project, Workspace workspace, CancellationToken ct)
        => ValueTask.FromResult(workspace.GitRef);
}

public enum CodexRole { Grounding, Planner, Engineer, Reviewer, PrimaryPrepare, PrimaryImplement }
public sealed record CodexRoleRequest(CodexRole Role, NodeInvocation Invocation, string Prompt,
    ImmutableArray<string> ProtectedFiles, string? SessionRef = null);
public interface ICodexRoleExecutor
{
    ValueTask<NodeResult> ExecuteAsync(CodexRoleRequest request, CancellationToken cancellationToken);
}
