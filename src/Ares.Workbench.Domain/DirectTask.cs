using System.Collections.Immutable;
namespace Ares.Workbench.Domain;

// Business records only. The external Primary owns native conversation and tools.
public enum DirectStage { Discussing, Ready, Implementing, Submitted, Checking, Rework, Blocked, AwaitingAcceptance, Done }
public sealed record OwnerEvidence(string Quote, string Source) {
    public void Validate() {
        if(string.IsNullOrWhiteSpace(Quote)||string.IsNullOrWhiteSpace(Source))
            throw new ArgumentException("Provide the actual Owner instruction and its conversation/message reference.");
    }
}
public sealed record AcceptanceCheck(int Criterion, string Kind, string Method);
public sealed record DirectAgreement(int Version, ReadyAnchors Anchors, ImmutableArray<AcceptanceCheck> Checks,
    string ProjectFingerprint, string WorkspaceStamp, ImmutableDictionary<string,string> BaselineFiles,
    OwnerEvidence Authorization, DateTimeOffset At, string BuildCommand = "", string TestCommand = "", DocumentSet? Documents = null);
public sealed record DirectTask(string PrimaryLabel, string? NativeSessionRef,
    DirectStage Stage, long Revision, ImmutableArray<DirectAgreement> Agreements,
    string? LastReport = null, string? SubmittedStamp = null, string? LastRunId = null,
    string? Note = null, OwnerEvidence? RiskApproval = null, OwnerEvidence? AcceptanceDecision = null,
    bool RequiresDocuments = false, DocumentSet? Documents = null, AlignmentRecord? Alignment = null, LensRecord? Lens = null, ImmutableArray<DocumentFile> VerifiedEvidence = default) {
    public ImmutableArray<DocumentFile> VerifiedEvidence {get;init;}=VerifiedEvidence.IsDefault?[]:VerifiedEvidence;
    public DirectAgreement? Agreement => Agreements.IsDefaultOrEmpty ? null : Agreements[^1];
}
