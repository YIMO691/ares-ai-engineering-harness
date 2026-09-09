using Ares.Workbench.Domain;
namespace Ares.Workbench.Application;
public interface IDirectDocuments {
    DocumentSet Write(string taskId,string title,int version,EngineeringBrief brief);
    void Validate(DocumentSet documents);
    DocumentSet Deliver(DocumentSet documents,string report);
    System.Collections.Immutable.ImmutableArray<DocumentFile> CaptureEvidence(IEnumerable<string> paths);
    void ValidateEvidence(System.Collections.Immutable.ImmutableArray<DocumentFile> files);
}
