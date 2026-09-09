using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Ares.Workbench.Domain;
namespace Ares.Workbench.Application;

// Business checkpoint only. Native conversation state remains owned by Codex.
public sealed record RunCheckpoint(string RunId, RunState State, string? CurrentStep, string? ResumeTarget,
    string? BlockedReason, string? BlockedCategory, ImmutableArray<string> CompletedSteps, int ReworkCount,
    string? PrimarySessionRef, string? ReviewerSessionRef, DateTimeOffset UpdatedAt,
    string TaskFingerprint, string RepoRoot, string WorkspaceStamp,
    int CodexInvocations = 0, int PrimaryInvocations = 0, int ReviewerInvocations = 0,
    int NativeReuses = 0, bool ResumeSupported = true)
{
    public static string Fingerprint(EngineeringTask task) => Convert.ToHexString(SHA256.HashData(
        Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { task.Goal,task.Acceptance,task.Constraints,task.Risk,task.WorkspaceId }))));
    public static string Category(string? code) => code switch {
        "PROJECT_POLICY" or "ROLE_BOUNDARY_VIOLATION" or "PROTECTED_FILE_CHANGED" => "PROJECT_POLICY",
        "PROJECT_CONFIGURATION" => "PROJECT_CONFIGURATION",
        "VERIFICATION_FAILED" => "EXTERNAL_COMMAND",
        "OWNER_INPUT_REQUIRED" or "REWORK_BUDGET" or "HUMAN_REJECTED" => "OWNER_INPUT_REQUIRED",
        _ => "EXECUTOR_ENVIRONMENT"
    };
    public RunCheckpoint WithRun(WorkflowRun run) => this with {
        State=run.State,CurrentStep=run.CurrentNode,
        ResumeTarget=run.State is RunState.Blocked or RunState.Paused ? run.CurrentNode : null,
        BlockedReason=run.Failure?.Message,BlockedCategory=run.Failure is null?null:Category(run.Failure.Code),
        CompletedSteps=[..run.Executions.GroupBy(e=>e.NodeId).Where(g=>g.Last().Result.Outcome==NodeOutcome.Succeeded).Select(g=>g.Key)],
        ReworkCount=run.ReworkCount,UpdatedAt=run.UpdatedAt
    };
}
