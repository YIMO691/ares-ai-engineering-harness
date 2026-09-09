using System.Collections.Immutable;
using System.Text.Json;
using Ares.Workbench.Application;
using Ares.Workbench.Domain;
using Ares.Workbench.Infrastructure;
using Ares.Workbench.Adapters.Codex;
namespace Ares.Workbench.Cli;
public static partial class DirectCli {
    private static async Task<EngineeringTask> AttachLens(DirectWorkflowService service,IWorkbenchStore store,RuntimeSettings settings,Request request,CancellationToken ct) {
        var task=store.Task(request.TaskId);
        if(task.Direct is not {Stage:DirectStage.AwaitingAcceptance} d||d.Revision!=request.Revision)
            throw new InvalidOperationException("Use current AwaitingAcceptance task revision for Change Lens.");
        var project=store.Project(task.WorkspaceId);
        var workspaces=new ProjectWorkspace(settings.DataRoot);var workspace=workspaces.Create(project,d.LastRunId!);
        var factory=new RunHandlerFactory(settings,store);
        var before=await factory.SnapshotAsync(project,workspace,ct);
        if(before.Fingerprint!=d.SubmittedStamp)throw new InvalidOperationException("Source changed after verification.");
        if(!Path.IsPathFullyQualified(settings.ChangeLensRoot)||!File.Exists(settings.PythonExecutable)||!File.Exists(settings.ChangeLensWorker))
            throw new ArgumentException("Configure PythonExecutable, ChangeLensRoot and the external ChangeLensWorker build.");
        if(string.IsNullOrWhiteSpace(request.Assembly))throw new ArgumentException("Assembly is required.");
        var lensRoot=WorkspacePolicy.ValidateRoot(settings.ChangeLensRoot);
        var script=WorkspacePolicy.Under(lensRoot,Path.Combine(lensRoot,"run_change_lens.py"));
        var output=LocalPaths.Output(Path.Combine(workspace.ArtifactRoot,"lens",Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(output);
        var env=settings.EnvironmentFor("lens-"+d.LastRunId).SetItem("PYTHONDONTWRITEBYTECODE","1")
            .SetItem("CHANGE_LENS_WORKER",LocalPaths.Output(settings.ChangeLensWorker)).SetItem("DOTNET_ROLL_FORWARD","Major")
            .SetItem("GIT_CONFIG_COUNT","1").SetItem("GIT_CONFIG_KEY_0","safe.directory").SetItem("GIT_CONFIG_VALUE_0",project.RepoRoot)
            .SetItem("PATH",Path.GetDirectoryName(settings.GitExecutable)+Path.PathSeparator+settings.EnvironmentFor("lens-"+d.LastRunId)["PATH"]);
        var intent=Path.Combine(output,"intent-evidence.json");
        await File.WriteAllTextAsync(intent,JsonSerializer.Serialize(new {
            schema_version="1.0.0",source="Ares agreement "+task.TaskId+"/v"+d.Agreement!.Version+"; "+d.Agreement.Authorization.Source,
            user_goal=task.Goal,ai_plan=d.Documents?.Brief.Increments??[d.Agreement.Anchors.KeyDecisions]
        }),ct);
        var baseCommit=d.Agreement.WorkspaceStamp.Split('|')[0];
        if(!System.Text.RegularExpressions.Regex.IsMatch(baseCommit,"^[0-9a-fA-F]{40,64}$"))
            throw new InvalidOperationException("Agreement has no immutable Git base.");
        var args=new List<string>{"-B",script,"explain",project.RepoRoot,request.UnityPath,"--assembly",request.Assembly,
            "--base",baseCommit,"--target","WORKTREE","--request-id",d.LastRunId!,"--intent-evidence",intent,
            "--output",Path.Combine(output,"change-story.html"),"--analysis-output",Path.Combine(output,"change-analysis.json"),
            "--story-output",Path.Combine(output,"change-story.json"),"--pretty"};
        if(request.AllowSyntaxPartial)args.Add("--allow-syntax-partial");
        var process=await new ProcessRunner([settings.PythonExecutable],[lensRoot]).RunAsync(
            new(settings.PythonExecutable,[..args],lensRoot,TimeSpan.FromMinutes(5),env,OutputLimit:200000),ct);
        await File.WriteAllTextAsync(Path.Combine(output,"process.json"),JsonSerializer.Serialize(process,Json),CancellationToken.None);
        var after=await factory.SnapshotAsync(project,workspace,CancellationToken.None);
        if(after.Fingerprint!=before.Fingerprint)throw new InvalidOperationException("Source changed during Change Lens; report is not attached.");
        var status="FAILED";
        if(process.Passed) {
            using var result=JsonDocument.Parse(process.Stdout);
            status=result.RootElement.GetProperty("status").GetString()??"UNKNOWN";
            foreach(var name in new[]{"change-story.html","change-story.json","change-analysis.json"})
                if(!File.Exists(Path.Combine(output,name)))throw new InvalidOperationException("Change Lens output incomplete.");
        }
        var files=Directory.GetFiles(output).Select(path=>new DocumentFile(Path.GetFileName(path),path,DirectDocuments.Hash(File.ReadAllText(path)),"")).ToImmutableArray();
        return await service.RecordLens(task.TaskId,d.Revision,new(d.LastRunId!,before.Fingerprint,status,
            "OLD="+baseCommit+" → submitted WORKTREE; includes pre-existing uncommitted changes relative to HEAD. Suggested checks are not executed tests.",files,DateTimeOffset.UtcNow),ct);
    }
}
