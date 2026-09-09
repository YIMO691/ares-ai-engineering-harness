using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Ares.Workbench.Application;
using Ares.Workbench.Domain;
namespace Ares.Workbench.Infrastructure;
public sealed class ProjectCommands(ProjectProfile project,Workspace workspace,string git,string dotnet,
    string scratch,ImmutableDictionary<string,string> environment)
{
    public ImmutableDictionary<string,string> Environment=>environment;
    public async Task<ProcessResult> Run(string command,CancellationToken ct)
    {
        var spec=CommandLineSpec.Parse(command);
        string executable=ResolveExecutable(spec.Executable);
        var args=spec.Arguments.Select(a=>a.Replace("{artifacts}",scratch)).ToList();
        if(Path.GetFileNameWithoutExtension(executable).Equals("dotnet",StringComparison.OrdinalIgnoreCase)
            && args.Any(a=>a is "build" or "test") && !args.Any(a=>a.Contains("AresArtifactsRoot",StringComparison.OrdinalIgnoreCase)))
            args.Add("-p:AresArtifactsRoot="+scratch);
        return await new ProcessRunner([executable],[workspace.RepoRoot]).RunAsync(new(executable,[..args],
            workspace.RepoRoot,TimeSpan.FromMinutes(3),environment,null,1000000),ct);
    }
    private string ResolveExecutable(string name)
    {
        if(name.Equals("dotnet",StringComparison.OrdinalIgnoreCase)||name.Equals("dotnet.exe",StringComparison.OrdinalIgnoreCase))return dotnet;
        if(Path.IsPathFullyQualified(name)&&File.Exists(name))return name;
        foreach(var path in (environment.GetValueOrDefault("PATH")??System.Environment.GetEnvironmentVariable("PATH")??"").Split(Path.PathSeparator))
            foreach(var suffix in new[]{"",".exe"}) {
                var candidate=Path.Combine(path,name+suffix);
                if(File.Exists(candidate))return Path.GetFullPath(candidate);
            }
        throw new ArgumentException("找不到可执行文件："+name+"。请使用完整路径或已安装的可执行程序。");
    }
    private async Task<string> Git(string[] args,CancellationToken ct)
    {
        var p=await new ProcessRunner([git],[workspace.RepoRoot]).RunAsync(new(git,[..args],workspace.RepoRoot,
            TimeSpan.FromSeconds(40),environment,null,1000000),ct);
        if(!p.Passed)throw new InvalidOperationException("Git 检查失败："+p.Stderr);
        return p.Stdout;
    }
    public async Task<string[]> Files(CancellationToken ct)
    {
        var output=await Git(["ls-files","--cached","--others","--exclude-standard","-z"],ct);
        return output.Split('\0',StringSplitOptions.RemoveEmptyEntries).Distinct().ToArray();
    }
    public async Task<Dictionary<string,string>> Snapshot(CancellationToken ct)
    {
        var result=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
        foreach(var relative in await Files(ct)) {
            var path=WorkspacePolicy.Under(workspace.RepoRoot,Path.Combine(workspace.RepoRoot,relative));
            if(File.Exists(path))result[relative]=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
        }
        return result;
    }
    public bool Writable(string path)
    {
        if(project.InstructionFiles.Contains(path,StringComparer.OrdinalIgnoreCase))return false;
        try{new WorkspacePolicy(workspace).Resolve(path);return true;}catch(UnauthorizedAccessException){return false;}
    }
    public async Task<string> Diff(CancellationToken ct)
    {
        var diff=await Git(["diff","--no-ext-diff","--no-textconv","HEAD","--","."],ct);
        var newFiles=await Git(["ls-files","--others","--exclude-standard","-z"],ct);
        foreach(var relative in newFiles.Split('\0',StringSplitOptions.RemoveEmptyEntries)) {
            var path=WorkspacePolicy.Under(workspace.RepoRoot,Path.Combine(workspace.RepoRoot,relative));
            if(new FileInfo(path).Length>200000){diff+="\nNew large file: "+relative+"\n";continue;}
            diff+="\ndiff --git a/"+relative+" b/"+relative+"\nnew file\n+++ b/"+relative+"\n"+
                string.Join("\n",File.ReadAllLines(path).Select(line=>"+"+line))+"\n";
        }
        return diff;
    }
}
