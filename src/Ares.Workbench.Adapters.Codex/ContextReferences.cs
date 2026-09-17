using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Ares.Workbench.Infrastructure;

namespace Ares.Workbench.Adapters.Codex;

// Complete, versioned text stays outside the target repository; prompts carry a bounded preview.
public sealed record ContextReference(string Label,string Path,string Sha256,int Bytes,string Preview,bool PreviewTruncated);

public sealed class ContextReferences(string artifactRoot)
{
    private readonly string root=LocalPaths.Output(System.IO.Path.Combine(artifactRoot,"context"));
    private readonly List<ContextReference> entries=[];
    public string[] ArtifactPaths=>entries.Select(x=>x.Path).Distinct().ToArray();

    public ContextReference Add(string label,string text,string? expectedHash=null)
    {
        var bytes=Encoding.UTF8.GetBytes(text);
        var hash=Convert.ToHexString(SHA256.HashData(bytes));
        if(expectedHash is not null&&!hash.Equals(expectedHash,StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Context content does not match its recorded version: "+label);
        var path=LocalPaths.Output(System.IO.Path.Combine(root,hash+".txt"));
        Directory.CreateDirectory(root);
        if(!File.Exists(path)) {
            using var stream=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.Read);
            stream.Write(bytes);
        }
        var reference=new ContextReference(label,path,hash,bytes.Length,text[..Math.Min(text.Length,1000)],text.Length>1000);
        Check(reference);entries.Add(reference);return reference;
    }

    public string Seal()=>Add("context-index",JsonSerializer.Serialize(entries.ToArray())).Path;

    public void Validate()
    {
        foreach(var reference in entries)Check(reference);
    }

    private static void Check(ContextReference reference)
    {
        var path=LocalPaths.Output(reference.Path);
        if(!File.Exists(path)||new FileInfo(path).Length!=reference.Bytes
            ||Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)))!=reference.Sha256)
            throw new InvalidDataException("Context reference changed or is missing: "+reference.Label);
    }
}
