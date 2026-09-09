using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using Ares.Workbench.Domain;
namespace Ares.Workbench.Infrastructure;
public sealed record ProcessRequest(string Executable, ImmutableArray<string> Arguments, string WorkingDirectory,
    TimeSpan Timeout, ImmutableDictionary<string,string> Environment, string? Stdin=null, int OutputLimit=100000);
public sealed record ProcessResult(int? ExitCode,string Stdout,string Stderr,bool TimedOut,bool Cancelled,bool Truncated,string? Failure, int? ProcessId=null, bool ProcessExited=false)
{
    public bool Passed=>ExitCode==0&&!TimedOut&&!Cancelled&&Failure is null;
}
public sealed class ProcessRunner(IEnumerable<string> allowedExecutables,IEnumerable<string> workingRoots)
{
    private readonly HashSet<string> executables=allowedExecutables.Select(Path.GetFullPath).ToHashSet(StringComparer.OrdinalIgnoreCase);
    private readonly string[] roots=workingRoots.Select(WorkspacePolicy.ValidateRoot).ToArray();
    public ProcessStartInfo CreateStartInfo(ProcessRequest request)
    {
        if(!Path.IsPathFullyQualified(request.Executable)||!executables.Contains(Path.GetFullPath(request.Executable))||!File.Exists(request.Executable))
            throw new UnauthorizedAccessException("Executable not in allow-list.");
        var name=Path.GetFileName(request.Executable).ToLowerInvariant();
        if(name is "cmd.exe" or "powershell.exe" or "pwsh.exe" or "bash.exe" or "sh.exe")throw new UnauthorizedAccessException("Shell executables denied.");
        if(request.Timeout<=TimeSpan.Zero||request.OutputLimit<1)throw new ArgumentException("Invalid process bounds.");
        var cwd=WorkspacePolicy.ValidateRoot(request.WorkingDirectory);
        if(!roots.Any(r=>IsUnder(r,cwd)))throw new UnauthorizedAccessException("Working directory denied.");
        var psi=new ProcessStartInfo(request.Executable){WorkingDirectory=cwd,UseShellExecute=false,CreateNoWindow=true,
            RedirectStandardOutput=true,RedirectStandardError=true,RedirectStandardInput=request.Stdin is not null,
            StandardOutputEncoding=Encoding.UTF8,StandardErrorEncoding=Encoding.UTF8};
        foreach(var arg in request.Arguments)psi.ArgumentList.Add(arg);
        // Preserve required OS/path settings, but do not inherit credentials into build/test children.
        foreach(var key in psi.Environment.Keys.ToArray())
            if(System.Text.RegularExpressions.Regex.IsMatch(key,"KEY|TOKEN|SECRET|PASSWORD",System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                psi.Environment.Remove(key);
        foreach(var pair in request.Environment)psi.Environment[pair.Key]=pair.Value;
        return psi;
    }
    private static bool IsUnder(string root,string p){try{WorkspacePolicy.Under(root,p);return true;}catch(UnauthorizedAccessException){return false;}}
    public async Task<ProcessResult> RunAsync(ProcessRequest request,CancellationToken ct=default)
    {
        var psi=CreateStartInfo(request);
        ct.ThrowIfCancellationRequested();
        using var process=new Process{StartInfo=psi};
        try{process.Start();}catch(Exception ex){return new(null,"","",false,false,false,ex.GetType().Name);}
        var truncated=false;
        async Task<string> Read(StreamReader reader)
        {
            var output=new StringBuilder();var buffer=new char[2048];
            while(true){int n=await reader.ReadAsync(buffer);if(n==0)break;
                int keep=Math.Min(n,request.OutputLimit-output.Length);
                if(keep>0)output.Append(buffer,0,keep);if(keep<n)truncated=true;}
            return JsonlEventSink.Redact(output.ToString());
        }
        var stdout=Read(process.StandardOutput);var stderr=Read(process.StandardError);
        using var timeout=new CancellationTokenSource(request.Timeout);
        using var linked=CancellationTokenSource.CreateLinkedTokenSource(ct,timeout.Token);
        bool cancelled=false,timedOut=false;string? failure=null;
        try
        {
            if(request.Stdin is not null){await process.StandardInput.WriteAsync(request.Stdin.AsMemory(),linked.Token);process.StandardInput.Close();}
            await process.WaitForExitAsync(linked.Token);
        }
        catch(OperationCanceledException)
        {
            cancelled=ct.IsCancellationRequested;timedOut=!cancelled;
            try{if(!process.HasExited)process.Kill(entireProcessTree:true);await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(10));}
            catch(Exception ex){failure="Termination:"+ex.GetType().Name;}
        }
        var output=await stdout.WaitAsync(TimeSpan.FromSeconds(10));
        var error=await stderr.WaitAsync(TimeSpan.FromSeconds(10));
        return new(process.HasExited?process.ExitCode:null,output,error,timedOut,cancelled,truncated,failure,process.Id,process.HasExited);
    }
}
