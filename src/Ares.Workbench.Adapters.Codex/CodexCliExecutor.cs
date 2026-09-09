using System.Collections.Immutable;
using System.Text.Json;
using System.Security.Cryptography;
using Ares.Workbench.Application;
using Ares.Workbench.Domain;
using Ares.Workbench.Infrastructure;
namespace Ares.Workbench.Adapters.Codex;

public sealed record CodexConfiguration(string Executable,string CodexHome,
    ImmutableDictionary<string,string> Environment,ImmutableArray<string> ProtectedFiles,string? Model=null);
public sealed class CodexCliExecutor(CodexConfiguration configuration,IEventSink events) : ICodexExecutor
{
    public async ValueTask<NodeResult> ExecuteAsync(NodeInvocation i,CancellationToken ct)
    {
        var policy=new WorkspacePolicy(i.Workspace);
        var protectedBefore=configuration.ProtectedFiles.ToDictionary(p=>p,p=>SHA256.HashData(File.ReadAllBytes(policy.Resolve(p))));
        var artifactRoot=WorkspacePolicy.ValidateRoot(i.Workspace.ArtifactRoot);Directory.CreateDirectory(artifactRoot);
        string Artifact(string name)=>WorkspacePolicy.Under(artifactRoot,Path.Combine(artifactRoot,name));
        var attempt=i.Run.Executions.Count(x=>x.NodeId==i.Node.NodeId)+1;
        var schema=Artifact("codex-output.schema.json");
        await File.WriteAllTextAsync(schema,"""{"type":"object","properties":{"status":{"type":"string","enum":["completed","blocked"]},"summary":{"type":"string"}},"required":["status","summary"],"additionalProperties":false}""",ct);
        var last=Artifact($"codex.r{i.Run.ReworkCount}.result.json");
        var args=new List<string>{"-a","never","exec","--sandbox","workspace-write","--ephemeral","--ignore-user-config",
            "--json","--color","never","-C",i.Workspace.RepoRoot,"--output-schema",schema,"--output-last-message",last};
        if(OperatingSystem.IsWindows()){args.Add("-c");args.Add("windows.sandbox=\"elevated\"");}
        if(configuration.Model is not null){args.Add("-m");args.Add(configuration.Model);}
        args.Add("-");
        var prompt="Implement the supplied engineering task in this repository. Do not call other agents, change workflow state, commit, push or access other repositories. "+
            "Write only allowed source paths; never modify protected files or project configuration. Do not run builds/tests: the Workbench runs independent checks after you return. "+
            "Use read/edit tools. Process caches and outputs must stay in the provided task scratch paths. Return JSON with status completed only when the requested edits are done, or blocked when tools/policy prevent completion, and a factual summary.\n"+
            JsonSerializer.Serialize(new { task=i.Task,workspace=i.Workspace,context=i.Context,rework_count=i.Run.ReworkCount,
                protected_files=configuration.ProtectedFiles,review_feedback=i.Context.PreviousResults.LastOrDefault(x=>x.Result.Outcome==NodeOutcome.ReworkRequired)?.Result.Output });
        // Windows native file-helper compatibility: use the same sandboxed shell path
        // proven by the direct probe; no change to workspace or approval permissions.
        if(OperatingSystem.IsWindows())prompt+="\nFor this Windows integration, use the sandboxed PowerShell shell tool to read and edit the allowed source files (Get-Content / System.IO.File). The apply_patch filesystem helper has a reproducible nested initialization error, so do not use that helper. Resolve paths under the current repository, verify the saved contents, and stop if the shell sandbox denies access. Never request elevation or change sandbox settings.";
        var environment=configuration.Environment.SetItem("CODEX_HOME",configuration.CodexHome);
        var runner=new ProcessRunner([configuration.Executable],[i.Workspace.RepoRoot]);
        await events.AppendAsync(new(Guid.NewGuid().ToString("N"),"ToolCalled",DateTimeOffset.UtcNow,i.Task.TaskId,i.Run.RunId,
            i.Node.NodeId,attempt,new Dictionary<string,string>{{"tool","CodexCli"},{"mode","workspace-write"}}.ToImmutableDictionary()),ct);
        var p=await runner.RunAsync(new(configuration.Executable,[..args],i.Workspace.RepoRoot,i.Node.Timeout,environment,prompt,300000),ct);
        var trace=Artifact($"codex.r{i.Run.ReworkCount}.jsonl");
        var stderr=Artifact($"codex.r{i.Run.ReworkCount}.stderr.txt");
        await File.WriteAllTextAsync(trace,p.Stdout,ct);await File.WriteAllTextAsync(stderr,p.Stderr,ct);
        foreach(var pair in protectedBefore)
            if(!File.Exists(policy.Resolve(pair.Key))||!SHA256.HashData(File.ReadAllBytes(policy.Resolve(pair.Key))).SequenceEqual(pair.Value))
                return NodeResult.Fail("PROTECTED_FILE_CHANGED","Codex changed protected input; delivery blocked.",false,EffectStatus.Unknown);
        if(p.Cancelled)return new(NodeOutcome.Cancelled,"node-result/v1","Codex cancelled.",[trace,stderr]);
        if(!p.Passed)return NodeResult.Fail(p.TimedOut?"CODEX_TIMEOUT":"CODEX_EXECUTION_FAILED","See Codex output artifacts.",false,EffectStatus.Unknown);
        if(!File.Exists(last))return NodeResult.Fail("CODEX_OUTPUT_MISSING","Missing structured final output.",false,EffectStatus.Unknown);
        try
        {
            using var doc=JsonDocument.Parse(await File.ReadAllTextAsync(last,ct));
            var summary=doc.RootElement.TryGetProperty("summary",out var summaryValue)&&summaryValue.ValueKind==JsonValueKind.String?summaryValue.GetString():null;
            var status=doc.RootElement.TryGetProperty("status",out var statusValue)&&statusValue.ValueKind==JsonValueKind.String?statusValue.GetString():null;
            if(string.IsNullOrWhiteSpace(summary))throw new JsonException();
            if(status=="blocked")return NodeResult.Fail("CODEX_BLOCKED",summary,false,EffectStatus.Unknown) with {ArtifactRefs=[trace,stderr,last]};
            if(status!="completed")throw new JsonException();
            return NodeResult.Success(summary,trace,stderr,last);
        }
        catch(JsonException){return NodeResult.Fail("CODEX_OUTPUT_INVALID","Invalid structured output.",false,EffectStatus.Unknown);}
    }
}
