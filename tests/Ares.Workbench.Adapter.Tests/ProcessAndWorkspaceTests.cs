using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.Json;
using Ares.Workbench.Domain;
using Ares.Workbench.Infrastructure;
using Ares.Workbench.Application;
using Ares.Workbench.Adapters.Codex;
namespace Ares.Workbench.Adapter.Tests;
public class ProcessAndWorkspaceTests
{
    private static string Root()
    {
        var root=Path.Combine(Path.GetTempPath(),"ares-adapter-tests",Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);return root;
    }
    private static string Dotnet=>Environment.GetEnvironmentVariable("ARES_DOTNET")??throw new InvalidOperationException("ARES_DOTNET required.");
    private static string Helper=>Environment.GetEnvironmentVariable("ARES_TEST_HELPER")??throw new InvalidOperationException("ARES_TEST_HELPER required.");
    private static ProcessRequest Request(string root,string mode,params string[] extra)=>
        new(Dotnet,[Helper,mode,..extra],root,TimeSpan.FromSeconds(10),ImmutableDictionary<string,string>.Empty);
    [Fact]public async Task ArgumentsRemainLiteralAndStreamsAreCaptured()
    {
        var root=Root();var runner=new ProcessRunner([Dotnet],[root]);
        var request=Request(root,"echo","a & echo injected","$(not-a-command)","quote\" spaced");
        var psi=runner.CreateStartInfo(request);
        Assert.False(psi.UseShellExecute);Assert.True(psi.CreateNoWindow);Assert.Equal(root,psi.WorkingDirectory);
        Assert.Equal("a & echo injected",psi.ArgumentList[2]);
        var result=await runner.RunAsync(request);
        Assert.True(result.Passed);Assert.Contains("stderr-value",result.Stderr);
        Assert.Equal(new[]{"a & echo injected","$(not-a-command)","quote\" spaced"},JsonSerializer.Deserialize<string[]>(result.Stdout));
    }
    [Fact]public async Task NonzeroExitCannotPass()
    {
        var root=Root();var result=await new ProcessRunner([Dotnet],[root]).RunAsync(Request(root,"exit"));
        Assert.Equal(7,result.ExitCode);Assert.False(result.Passed);
    }
    [Fact]public async Task BothStreamsAreDrainedButRetainedOutputIsBounded()
    {
        var root=Root();var result=await new ProcessRunner([Dotnet],[root]).RunAsync(Request(root,"flood") with {OutputLimit=1024});
        Assert.True(result.Passed);Assert.True(result.Truncated);Assert.Equal(1024,result.Stdout.Length);Assert.Equal(1024,result.Stderr.Length);
    }
    [Fact]public async Task TimeoutKillsProcess()
    {
        var root=Root();var result=await new ProcessRunner([Dotnet],[root]).RunAsync(Request(root,"hang") with {Timeout=TimeSpan.FromMilliseconds(500)});
        Assert.True(result.TimedOut);Assert.False(result.Cancelled);Assert.False(result.Passed);
    }
    [Fact]public async Task CancellationWorksEvenWhenTimeoutIsConfigured()
    {
        var root=Root();using var cts=new CancellationTokenSource(500);
        var timer=Stopwatch.StartNew();
        var result=await new ProcessRunner([Dotnet],[root]).RunAsync(Request(root,"hang") with {Timeout=TimeSpan.FromSeconds(20)},cts.Token);
        Assert.True(result.Cancelled);Assert.False(result.TimedOut);Assert.True(timer.Elapsed<TimeSpan.FromSeconds(8));
    }
    [Fact]public async Task TimeoutReapsChildProcess()
    {
        var root=Root();var result=await new ProcessRunner([Dotnet],[root]).RunAsync(Request(root,"child") with {Timeout=TimeSpan.FromSeconds(1)});
        Assert.True(result.TimedOut);
        var pid=int.Parse(result.Stdout.Trim());
        try{using var child=Process.GetProcessById(pid);Assert.True(child.HasExited);}catch(ArgumentException){ }
    }
    [Fact]public void UnknownExecutableAndCwdAreRejected()
    {
        var root=Root();var runner=new ProcessRunner([Dotnet],[root]);
        Assert.Throws<UnauthorizedAccessException>(()=>runner.CreateStartInfo(Request(root,"echo") with {Executable="not-absolute"}));
        Assert.Throws<UnauthorizedAccessException>(()=>runner.CreateStartInfo(Request(root,"echo") with {WorkingDirectory=Path.GetTempPath()}));
    }
    [Fact]public void WorkspaceRequiresAllowedRelativePath()
    {
        var root=Root();Directory.CreateDirectory(Path.Combine(root,"src"));
        File.WriteAllText(Path.Combine(root,"src","a.cs"),"class A {}");
        var policy=new WorkspacePolicy(new("w",root,"HEAD",["src"],root,"local"));
        Assert.Equal("class A {}",policy.Read("src/a.cs"));
        Assert.Single(policy.Search("class",["src/a.cs"]));
        foreach(var path in new[]{"../escape","src/../../escape",".git/config","other/file"})
            Assert.Throws<UnauthorizedAccessException>(()=>policy.Resolve(path));
        Assert.Throws<UnauthorizedAccessException>(()=>policy.Resolve(Path.Combine(root,"src","a.cs")));
    }
    [Theory][InlineData("blocked",NodeOutcome.Failed,"CODEX_BLOCKED")][InlineData("unknown",NodeOutcome.Failed,"CODEX_OUTPUT_INVALID")][InlineData("completed",NodeOutcome.Succeeded,null)]
    public async Task CodexExitZeroRequiresExplicitCompletion(string status,NodeOutcome outcome,string? code)
    {
        var root=Root();var workspace=new Workspace("w",root,"HEAD",["."],root,"local");
        var now=DateTimeOffset.UtcNow;
        var task=new EngineeringTask("t","test","goal",["done"],[],Risk.Standard,"w",TaskLifecycle.Active,now,now);
        var definition=WorkflowCatalog.Create(Risk.Standard);
        var run=new WorkflowRun("r","t",definition.WorkflowId,definition.Version,"test",RunState.Running,"codex",0,now,now,1,[],null,null);
        var context=new ContextPackage(task,workspace,ImmutableDictionary<string,string>.Empty,[],[],[],[]);
        var environment=ImmutableDictionary<string,string>.Empty.SetItem("ARES_FAKE_CODEX_STATUS",status);
        var executor=new CodexCliExecutor(new(Path.ChangeExtension(Helper,".exe"),root,environment,[]),new JsonlEventSink(root));
        var result=await executor.ExecuteAsync(new(definition.Node("codex"),task,run,workspace,context),default);
        Assert.Equal(outcome,result.Outcome);Assert.Equal(code,result.Failure?.Code);
        if(status=="blocked"){Assert.Equal(EffectStatus.Unknown,result.Failure!.Effect);Assert.Equal(3,result.ArtifactRefs.Length);}
    }
    [Fact]public void RedactionPreservesTaskIdentifiers()
    {
        Assert.Equal("TASK-20260907-workbench-phase1",JsonlEventSink.Redact("TASK-20260907-workbench-phase1"));
        Assert.Equal("[REDACTED]",JsonlEventSink.Redact("sk-secret12345678"));
    }
    [Fact]public async Task JsonlIsAppendOnlyObservableData()
    {
        var root=Root();var sink=new JsonlEventSink(root);
        var payload=new Dictionary<string,string>{{"token","Bearer fake-token"}}.ToImmutableDictionary();
        for(int i=0;i<2;i++)await sink.AppendAsync(new(i.ToString(),"ToolCalled",DateTimeOffset.UtcNow,"t","r","n",1,payload));
        var lines=File.ReadAllLines(Path.Combine(root,"events.jsonl"));
        Assert.Equal(2,lines.Length);Assert.All(lines,line=>{using var doc=JsonDocument.Parse(line);Assert.Equal("r",doc.RootElement.GetProperty("RunId").GetString());Assert.DoesNotContain("fake-token",line);});
        Assert.Throws<KeyNotFoundException>(()=>new InMemoryRunStore().Get("r"));
    }
}
