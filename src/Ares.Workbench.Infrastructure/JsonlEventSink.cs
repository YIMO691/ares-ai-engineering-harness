using System.Text.Json;
using System.Text.RegularExpressions;
using Ares.Workbench.Application;
using Ares.Workbench.Domain;
namespace Ares.Workbench.Infrastructure;
public sealed class JsonlEventSink : IEventSink
{
    private readonly string path;private readonly SemaphoreSlim gate=new(1,1);
    public JsonlEventSink(string artifactRoot,string filename="events.jsonl")
    {
        var root=WorkspacePolicy.ValidateRoot(artifactRoot);
        path=WorkspacePolicy.Under(root,Path.Combine(root,filename));
        Directory.CreateDirectory(root);
    }
    public async ValueTask AppendAsync(WorkbenchEvent value,CancellationToken cancellationToken=default)
    {
        await gate.WaitAsync(cancellationToken);
        try{await File.AppendAllTextAsync(path,Redact(JsonSerializer.Serialize(value))+Environment.NewLine,cancellationToken);}
        finally{gate.Release();}
    }
    public static string Redact(string text)
    {
        foreach(System.Collections.DictionaryEntry e in Environment.GetEnvironmentVariables())
            if(Regex.IsMatch((string)e.Key,"KEY|TOKEN|SECRET|PASSWORD",RegexOptions.IgnoreCase)&&e.Value is string s&&s.Length>=8)
                text=text.Replace(s,"[REDACTED]",StringComparison.Ordinal);
        return Regex.Replace(text,@"(?i)(\bsk-[a-z0-9_-]{8,}|Bearer\s+[^\s""]+)","[REDACTED]");
    }
}
