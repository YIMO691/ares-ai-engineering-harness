using System.Collections.Immutable;
using Ares.Workbench.Infrastructure;
namespace Ares.Workbench.Adapters.Codex;
public sealed class RuntimeSettings
{
    public string DataRoot {get;set;} = "";
    public string ScratchRoot {get;set;} = "";
    public string CodexHome {get;set;} = "";
    public string NugetPackages {get;set;} = "";
    public string AuthSource {get;set;} = "";
    public string CodexExecutable {get;set;} = "";
    public string DotnetExecutable {get;set;} = "";
    public string GitExecutable {get;set;} = "";
    public string Model {get;set;} = "gpt-6-astra";
    public bool ObserverOnly {get;set;} = true;
    public void ValidateObserver() {
        if(string.IsNullOrWhiteSpace(DataRoot))throw new InvalidOperationException("Configure Workbench DataRoot.");
        DataRoot=LocalPaths.Output(DataRoot);Directory.CreateDirectory(DataRoot);
    }
    public void Validate()
    {
        if(new[]{DataRoot,ScratchRoot,CodexHome}.Any(string.IsNullOrWhiteSpace))throw new InvalidOperationException("Configure Workbench DataRoot, ScratchRoot and CodexHome in your local settings file.");
        DataRoot=LocalPaths.Output(DataRoot);ScratchRoot=LocalPaths.Output(ScratchRoot);CodexHome=LocalPaths.Output(CodexHome);
        foreach(var path in new[]{DataRoot,ScratchRoot,CodexHome})Directory.CreateDirectory(path);
        foreach(var path in new[]{CodexExecutable,DotnetExecutable,GitExecutable})
            if(!Path.IsPathFullyQualified(path)||!File.Exists(path))throw new InvalidOperationException("工具路径不可用："+path);
        if(!Path.GetFileName(CodexExecutable).Equals("codex.exe",StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("真实 Codex CLI 路径必须指向 codex.exe。");
    }
    public ImmutableDictionary<string,string> EnvironmentFor(string runId)
    {
        var root=LocalPaths.Output(Path.Combine(ScratchRoot,runId));Directory.CreateDirectory(root);
        var env=new Dictionary<string,string>();
        foreach(var name in new[]{"TEMP","TMP","TMPDIR","APPDATA","LOCALAPPDATA","DOTNET_CLI_HOME","NUGET_HTTP_CACHE_PATH","NUGET_PLUGINS_CACHE_PATH"}) {
            var path=LocalPaths.Output(Path.Combine(root,name.ToLowerInvariant()));Directory.CreateDirectory(path);env[name]=path;
        }
        env["DOTNET_ROOT"]=Path.GetDirectoryName(DotnetExecutable)!;
        env["DOTNET_CLI_TELEMETRY_OPTOUT"]="1";env["DOTNET_NOLOGO"]="1";env["DOTNET_ADD_GLOBAL_TOOLS_TO_PATH"]="false";
        env["NUGET_PACKAGES"]=string.IsNullOrWhiteSpace(NugetPackages)?LocalPaths.Output(Path.Combine(ScratchRoot,"nuget")):LocalPaths.Output(NugetPackages);
        env["PATH"]=Path.GetDirectoryName(DotnetExecutable)+Path.PathSeparator+System.Environment.GetEnvironmentVariable("PATH");
        return env.ToImmutableDictionary();
    }
}
