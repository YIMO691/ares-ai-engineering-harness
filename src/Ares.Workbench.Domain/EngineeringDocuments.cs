using System.Collections.Immutable;
namespace Ares.Workbench.Domain;

public sealed record ContextFact(string Observation,string Source);
public sealed record EngineeringBrief(string Level,string LevelReason,string Source,
    ImmutableArray<ContextFact> Context,ReadyAnchors Anchors,ImmutableArray<AcceptanceCheck> Checks,
    string Design,ImmutableArray<string> Increments);
public sealed record DocumentFile(string Role,string Path,string Sha256,string Content);
public sealed record DocumentSet(int Version,string SopVersion,EngineeringBrief Brief,ImmutableArray<DocumentFile> Files);
public sealed record CriterionEvidence(int Criterion,string Result,ImmutableArray<string> ArtifactIds,OwnerEvidence? ManualConfirmation=null);
public sealed record AlignmentInput(ImmutableArray<CriterionEvidence> Criteria,string DesignConformance,
    string Deviations,string Cleanup,ImmutableArray<string> Unresolved);
public sealed record AlignmentRecord(string RunId,string SourceStamp,AlignmentInput Input,DateTimeOffset At);
public sealed record LensRecord(string RunId,string SourceStamp,string Status,string Scope,ImmutableArray<DocumentFile> Files,DateTimeOffset At);
