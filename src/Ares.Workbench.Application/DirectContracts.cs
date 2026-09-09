using System.Collections.Immutable;
using Ares.Workbench.Domain;
namespace Ares.Workbench.Application;

public sealed record SourceSnapshot(string WorkspaceStamp, ImmutableDictionary<string,string> Files) {
    public string Fingerprint => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
        System.Text.Encoding.UTF8.GetBytes(WorkspaceStamp+"\n"+string.Join("\n",Files.OrderBy(p=>p.Key,StringComparer.Ordinal).Select(p=>p.Key+":"+p.Value)))));
}
public interface IDirectEvidence {
    ValueTask<SourceSnapshot> SnapshotAsync(ProjectProfile project, Workspace workspace, CancellationToken ct);
    ValueTask<string> DiffAsync(ProjectProfile project, Workspace workspace, CancellationToken ct);
    bool Writable(ProjectProfile project, Workspace workspace, string path);
}
