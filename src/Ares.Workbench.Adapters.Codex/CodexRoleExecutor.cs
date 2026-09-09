using System.Collections.Immutable;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using Ares.Workbench.Application;
using Ares.Workbench.Domain;
using Ares.Workbench.Infrastructure;
namespace Ares.Workbench.Adapters.Codex;

public sealed class CodexRoleExecutor(CodexConfiguration configuration,IEventSink events) : ICodexRoleExecutor
{
    public async ValueTask<NodeResult> ExecuteAsync(CodexRoleRequest request,CancellationToken ct)
    {
        var i=request.Invocation;
        bool primary=request.Role is CodexRole.PrimaryPrepare or CodexRole.PrimaryImplement or CodexRole.PrimaryDiscuss;
        bool writable=request.Role is CodexRole.Engineer or CodexRole.PrimaryImplement;
        bool nativeWrite=request.Role!=CodexRole.PrimaryDiscuss&&(primary||writable);
        bool reviewer=request.Role==CodexRole.Reviewer;
        var store=events as IWorkbenchStore;
        string label=primary?"Primary Codex":reviewer?"Independent Reviewer":request.Role.ToString();
        var root=WorkspacePolicy.ValidateRoot(i.Workspace.RepoRoot);
        var artifacts=LocalPaths.Output(i.Workspace.ArtifactRoot);Directory.CreateDirectory(artifacts);
        string FilePath(string name)=>WorkspacePolicy.Under(artifacts,Path.Combine(artifacts,name));
        string stem=request.Role.ToString().ToLowerInvariant()+"-r"+i.Run.ReworkCount+"-a"+(i.Run.Executions.Count(e=>e.NodeId==i.Node.NodeId)+1)+"-"+Guid.NewGuid().ToString("N")[..8];
        var protectedBefore=request.ProtectedFiles.ToDictionary(p=>p,p=>SHA256.HashData(File.ReadAllBytes(WorkspacePolicy.Under(root,Path.Combine(root,p)))));
        string schema=FilePath(stem+".schema.json");
        string reviewProperties=request.Role==CodexRole.Reviewer?",\"verdict\":{\"type\":\"string\",\"enum\":[\"PASS\",\"REWORK_REQUIRED\"]},\"findings\":{\"type\":\"array\",\"items\":{\"type\":\"object\",\"properties\":{\"severity\":{\"type\":\"string\"},\"requirement\":{\"type\":\"string\"},\"issue\":{\"type\":\"string\"},\"evidence\":{\"type\":\"string\"},\"requested_fix\":{\"type\":\"string\"}},\"required\":[\"severity\",\"requirement\",\"issue\",\"evidence\",\"requested_fix\"],\"additionalProperties\":false}}":"";
        string prepareProperties=request.Role==CodexRole.PrimaryPrepare?",\"grounding\":{\"type\":\"string\"},\"plan\":{\"type\":\"string\"},\"required_write_paths\":{\"type\":\"array\",\"items\":{\"type\":\"string\"}}":"";
        string prepareRequired=request.Role==CodexRole.PrimaryPrepare?",\"grounding\",\"plan\",\"required_write_paths\"":"";
        string discussionProperties=request.Role==CodexRole.PrimaryDiscuss ?
            ",\"ready\":{\"type\":\"object\",\"properties\":{\"Goal\":{\"type\":\"string\"},\"Acceptance\":{\"type\":\"array\",\"items\":{\"type\":\"string\"}},\"NonGoals\":{\"type\":\"string\"},\"Boundary\":{\"type\":\"string\"},\"KeyDecisions\":{\"type\":\"string\"},\"Verification\":{\"type\":\"string\"},\"Unresolved\":{\"type\":\"array\",\"items\":{\"type\":\"string\"}}},\"required\":[\"Goal\",\"Acceptance\",\"NonGoals\",\"Boundary\",\"KeyDecisions\",\"Verification\",\"Unresolved\"],\"additionalProperties\":false}" : "";
        string categoryProperty=",\"blocked_category\":{\"type\":\"string\",\"enum\":[\"\",\"PROJECT_POLICY\",\"PROJECT_CONFIGURATION\",\"EXECUTOR_ENVIRONMENT\",\"OWNER_INPUT_REQUIRED\",\"EXTERNAL_COMMAND\"]}";
        var schemaText="{\"type\":\"object\",\"properties\":{\"status\":{\"type\":\"string\",\"enum\":[\"completed\",\"blocked\"]},\"summary\":{\"type\":\"string\"},\"content\":{\"type\":\"string\"}"+reviewProperties+prepareProperties+discussionProperties+categoryProperty+"},\"required\":[\"status\",\"summary\",\"content\",\"blocked_category\""+prepareRequired+(request.Role==CodexRole.PrimaryDiscuss?",\"ready\"":"")+(request.Role==CodexRole.Reviewer?",\"verdict\",\"findings\"":"")+"],\"additionalProperties\":false}";
        await System.IO.File.WriteAllTextAsync(schema,schemaText,ct);
        string result=FilePath(stem+".json");
        var args=new List<string>{"-a","never","-s",nativeWrite?"workspace-write":"read-only","-C",root,"exec"};
        if(request.SessionRef is not null)args.Add("resume");
        args.AddRange(["--ignore-user-config","--json","--output-schema",schema,"--output-last-message",result]);
        if(!primary && !reviewer)args.Add("--ephemeral"); // legacy caller only
        if(OperatingSystem.IsWindows()){args.Add("-c");args.Add("windows.sandbox=\"elevated\"");}
        if(configuration.Model is not null){args.Add("-m");args.Add(configuration.Model);}
        if(request.SessionRef is not null)args.Add(request.SessionRef);
        args.Add("-");
        var prompt=$"You are {label} in Ares Workbench. The Application owns Task/Run state; never claim delivery or change workflow metadata. "+
            "Do not start other agents, commit, push, access other repositories, change credentials or elevate permissions. "+
            (writable?"Edit only allowed paths; preserve instruction files and protected files. Workbench independently runs tests; do not run builds/tests yourself. ":"READ ONLY: inspect and reason, but do not edit source, configuration or artifacts and do not run builds/tests. ")+
            "Return structured output; content is your concise Markdown report, written to artifacts by the host. Set status blocked if tools fail and choose blocked_category; otherwise use empty blocked_category. "+
            (request.Role==CodexRole.PrimaryPrepare?"This is PRIMARY_PREPARE, not implementation. Produce grounding, plan and exact repository-relative required_write_paths covering ALL requested implementation and test edits, even if current allowed_paths omit them. A policy mismatch is recorded in the plan; return completed preparation without edits so the host can block PRIMARY_IMPLEMENT. ":"")+
            (request.SessionRef is not null?"Continue this native session. The CURRENT PROJECT POLICY and task below supersede earlier project configuration. Do not redo completed preparation. ":"")+
            (OperatingSystem.IsWindows()?"Use the sandboxed PowerShell shell tool for reading"+(writable?" and editing":"")+" files; the native apply_patch filesystem helper has a known initialization failure in this environment. Never bypass a sandbox denial. ":"")+
            "\n"+request.Prompt;
        var timer=Stopwatch.StartNew();
        await events.AppendAsync(new(Guid.NewGuid().ToString("N"),"RoleStarted",DateTimeOffset.UtcNow,i.Task.TaskId,i.Run.RunId,i.Node.NodeId,i.Run.ReworkCount+1,
            new Dictionary<string,string>{{"role",label},{"permission",nativeWrite?"workspace-write":"read-only"},{"artifact",stem}}.ToImmutableDictionary()),ct);
        if(store?.Checkpoint(i.Run.RunId) is not null)store.UpdateCheckpoint(i.Run.RunId,c=>c with {
            CodexInvocations=c.CodexInvocations+1,PrimaryInvocations=c.PrimaryInvocations+(primary?1:0),
            ReviewerInvocations=c.ReviewerInvocations+(reviewer?1:0)});
        var environment=configuration.Environment.SetItem("CODEX_HOME",configuration.CodexHome);
        var process=await new ProcessRunner([configuration.Executable],[root]).RunAsync(new(configuration.Executable,[..args],root,i.Node.Timeout,environment,prompt,1000000),ct);
        timer.Stop();
        var observed=NativeThreadReference(process.Stdout);
        bool reused=request.SessionRef is not null&&request.SessionRef==observed;
        if(store?.Checkpoint(i.Run.RunId) is not null && observed is not null && (request.SessionRef is null || reused))
            store.UpdateCheckpoint(i.Run.RunId,c=>c with {
                PrimarySessionRef=primary?observed:c.PrimarySessionRef,ReviewerSessionRef=reviewer?observed:c.ReviewerSessionRef,
                NativeReuses=c.NativeReuses+(reused?1:0)});
        await events.AppendAsync(new(Guid.NewGuid().ToString("N"),"CodexInvocationFinished",DateTimeOffset.UtcNow,
            i.Task.TaskId,i.Run.RunId,i.Node.NodeId,null,new Dictionary<string,string>{
                {"role",label},{"session_ref",observed??""},{"native_session_reused",reused.ToString()},
                {"cancelled",process.Cancelled.ToString()},{"process_exited",process.ProcessExited.ToString()}
            }.ToImmutableDictionary()),CancellationToken.None);
        string stdout=FilePath(stem+".stdout.jsonl"),stderr=FilePath(stem+".stderr.txt"),meta=FilePath(stem+".process.json");
        await System.IO.File.WriteAllTextAsync(stdout,process.Stdout,CancellationToken.None);
        await System.IO.File.WriteAllTextAsync(stderr,process.Stderr,CancellationToken.None);
        await System.IO.File.WriteAllTextAsync(meta,JsonSerializer.Serialize(new{role=label,permission=nativeWrite?"workspace-write":"read-only",session_ref=observed,requested_session_ref=request.SessionRef,native_session_reused=reused,process.ProcessId,process.ProcessExited,duration_ms=timer.ElapsedMilliseconds,process.ExitCode,process.TimedOut,process.Cancelled,process.Failure,process.Truncated,executable=configuration.Executable,arguments=args},new JsonSerializerOptions{WriteIndented=true}),CancellationToken.None);
        var refs=new List<string>{stdout,stderr,meta,schema};
        if(System.IO.File.Exists(result))refs.Add(result);
        NodeResult Failed(string code,string message)=>NodeResult.Fail(code,message,false,EffectStatus.Unknown) with {ArtifactRefs=[..refs]};
        foreach(var pair in protectedBefore) {
            var path=WorkspacePolicy.Under(root,Path.Combine(root,pair.Key));
            if(!System.IO.File.Exists(path)||!SHA256.HashData(System.IO.File.ReadAllBytes(path)).SequenceEqual(pair.Value))
                return Failed("PROTECTED_FILE_CHANGED","角色修改了受保护的文件："+pair.Key);
        }
        if(process.Cancelled)return new(NodeOutcome.Cancelled,"node-result/v1","Executor interrupted; partial source changes retained.",[..refs]);
        if((primary||reviewer) && observed is null && process.Passed)return Failed("CODEX_SESSION_MISSING","Codex 未提供原生 session ID，不能确认续接。");
        if(request.SessionRef is not null && observed!=request.SessionRef)return Failed("CODEX_SESSION_MISMATCH","Codex did not confirm the requested native session ID.");
        if(!process.Passed)return Failed(process.TimedOut?"CODEX_TIMEOUT":"CODEX_PROCESS_FAILED","Codex 未成功结束，请查看保存的 stdout/stderr。");
        if(!System.IO.File.Exists(result))return Failed("CODEX_OUTPUT_MISSING","Codex 缺少结构化结果。");
        try {
            using var doc=JsonDocument.Parse(await System.IO.File.ReadAllTextAsync(result,ct));
            var data=doc.RootElement;
            var status=data.GetProperty("status").GetString();
            var summary=data.GetProperty("summary").GetString()??"";
            var content=data.GetProperty("content").GetString()??"";
            string md=FilePath(stem+".md");await System.IO.File.WriteAllTextAsync(md,JsonlEventSink.Redact(content),ct);refs.Add(md);
            // Last-message output also passes through the same redaction boundary.
            await System.IO.File.WriteAllTextAsync(result,JsonlEventSink.Redact(data.GetRawText()),ct);
            if(status!="completed")return Failed(data.TryGetProperty("blocked_category",out var category)&&!string.IsNullOrWhiteSpace(category.GetString())?category.GetString()!:"CODEX_BLOCKED",summary);
            if(request.Role==CodexRole.PrimaryPrepare) {
                foreach(var section in new[]{"grounding","plan"}) {
                    var sectionText=data.GetProperty(section).GetString();
                    if(string.IsNullOrWhiteSpace(sectionText))return Failed("CODEX_OUTPUT_INVALID","Primary prepare 缺少 "+section);
                    var sectionPath=FilePath(section+".md");
                    await System.IO.File.WriteAllTextAsync(sectionPath,JsonlEventSink.Redact(sectionText),ct);refs.Add(sectionPath);
                }
                var requiredPath=FilePath("required-write-paths.json");
                await System.IO.File.WriteAllTextAsync(requiredPath,data.GetProperty("required_write_paths").GetRawText(),ct);refs.Add(requiredPath);
            }
            if(string.IsNullOrWhiteSpace(content))return Failed("CODEX_OUTPUT_INVALID","角色返回了空报告。");
            if(request.Role==CodexRole.Reviewer) {
                var verdict=data.GetProperty("verdict").GetString();
                if(verdict=="REWORK_REQUIRED") {
                    if(data.GetProperty("findings").GetArrayLength()==0)return Failed("REVIEW_INVALID","返工缺少可操作的 findings。");
                    return NodeResult.Rework(JsonlEventSink.Redact(data.GetRawText()),[..refs]);
                }
                if(verdict!="PASS")return Failed("REVIEW_INVALID","Reviewer verdict 无效。");
            }
            return NodeResult.Success(JsonlEventSink.Redact(request.Role==CodexRole.PrimaryDiscuss?data.GetRawText():content),[..refs]);
        } catch(JsonException){return Failed("CODEX_OUTPUT_INVALID","无法解析角色输出，原始输出已保留。");}
        catch(KeyNotFoundException){return Failed("CODEX_OUTPUT_INVALID","角色输出缺少必需字段，原始输出已保留。");}
    }
    public static string? NativeThreadReference(string jsonl)
    {
        foreach(var line in jsonl.Split('\n',StringSplitOptions.RemoveEmptyEntries)) {
            try { using var doc=JsonDocument.Parse(line);var e=doc.RootElement;
                if(e.TryGetProperty("type",out var type)&&type.GetString()=="thread.started"&&e.TryGetProperty("thread_id",out var id)
                    &&id.ValueKind==JsonValueKind.String&&Guid.TryParse(id.GetString(),out _))return id.GetString();
            } catch(JsonException) { }
        }
        return null;
    }

}
