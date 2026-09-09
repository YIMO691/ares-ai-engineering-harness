using System.Collections.Immutable;
namespace Ares.Workbench.Domain;

// Visible business discussion and frozen decisions only. Native conversation/tool state stays in Codex.
public sealed record ReadyAnchors(string Goal, ImmutableArray<string> Acceptance, string NonGoals,
    string Boundary, string KeyDecisions, string Verification, ImmutableArray<string> Unresolved)
{
    public static ReadyAnchors Empty => new("",[],"","","","",[]);
    public void Validate()
    {
        if(new[]{Goal,NonGoals,Boundary,KeyDecisions,Verification}.Any(string.IsNullOrWhiteSpace) ||
            Acceptance.IsDefaultOrEmpty || Acceptance.Any(string.IsNullOrWhiteSpace) || Unresolved.IsDefault || !Unresolved.IsEmpty)
            throw new InvalidOperationException("Ready 需要完整的六项约束、至少一条验收标准，且 Unresolved 必须为空。");
    }
}
public sealed record DiscussionTurn(string Speaker, string Text, DateTimeOffset At);
public sealed record ReadySnapshot(int Version, ReadyAnchors Anchors, DateTimeOffset FrozenAt,
    string PrimarySessionRef, string ProjectFingerprint, string WorkspaceStamp);
public sealed record FusionTask(ReadyAnchors Draft, ImmutableArray<DiscussionTurn> Turns,
    ImmutableArray<ReadySnapshot> Snapshots, string? PrimarySessionRef = null,
    long Revision = 0, string DiscussionState = "Idle", string? Error = null)
{
    public ReadySnapshot? Frozen => Snapshots.IsDefaultOrEmpty ? null : Snapshots[^1];
}
