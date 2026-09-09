using System.Collections.Immutable;
using System.Text.Json;
using Ares.Workbench.Domain;
using Ares.Workbench.Application;
using Ares.Workbench.Infrastructure;
using Ares.Workbench.Adapters.Codex;
using Ares.Workbench.Adapters.AgentFramework;
namespace Ares.Workbench.Cli;

public static partial class DirectCli
{
    private static readonly JsonSerializerOptions Json=new(SqliteWorkbenchStore.Json){PropertyNameCaseInsensitive=true};
    public sealed record Request {
        public string TaskId {get;init;}="";
        public long Revision {get;init;}=-1;
        public string ProjectId {get;init;}="";
        public ProjectProfile? Project {get;init;}
        public string Title {get;init;}="";
        public Risk Risk {get;init;}=Risk.Standard;
        public string PrimaryLabel {get;init;}="";
        public string? NativeSessionRef {get;init;}
        public ReadyAnchors? Anchors {get;init;}
        public ImmutableArray<AcceptanceCheck> Checks {get;init;}=[];
        public OwnerEvidence? Owner {get;init;}
        public string Message {get;init;}="";
        public EngineeringBrief? Brief {get;init;}
        public AlignmentInput? Alignment {get;init;}
        public string UnityPath {get;init;}="";
        public string Assembly {get;init;}="";
        public bool AllowSyntaxPartial {get;init;}
    }
    public static async Task<int> Run(string[] args) {
        Console.OutputEncoding=System.Text.Encoding.UTF8;
        if(args.Length is <2 or >3) {
            Console.Error.WriteLine("Usage: direct <settings.json> <projects|project|create|document|list|status|agree|begin|submit|verify|feedback|reopen|align|lens|accept|recover> [request.json]");
            return 2;
        }
        using var cancellation=new CancellationTokenSource();
        ConsoleCancelEventHandler cancel=(_,e)=>{e.Cancel=true;cancellation.Cancel();};
        Console.CancelKeyPress+=cancel;
        try {
            using var doc=JsonDocument.Parse(await File.ReadAllTextAsync(Path.GetFullPath(args[0])));
            var section=doc.RootElement.TryGetProperty("Workbench",out var nested)?nested:doc.RootElement;
            var settings=section.Deserialize<RuntimeSettings>(Json)??throw new ArgumentException("Missing settings.");
            settings.ValidateObserver();
            string operation=args[1];
            var readOnly=operation is "projects" or "list" or "status";
            using var lease=readOnly?null:new FileStream(LocalPaths.Output(Path.Combine(settings.DataRoot,".writer.lock")),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
            var store=new SqliteWorkbenchStore(settings.DataRoot);
            var request=args.Length==3?JsonSerializer.Deserialize<Request>(await File.ReadAllTextAsync(Path.GetFullPath(args[2])),Json)??new():new();
            if(readOnly) {
                object output=operation switch {
                    "projects"=>store.Projects(),
                    "list"=>store.Tasks(),
                    _=>new {task=store.Task(request.TaskId),runs=store.Runs().Where(r=>r.TaskId==request.TaskId),events=store.Events(request.TaskId),
                        artifacts=store.Runs().Where(r=>r.TaskId==request.TaskId).SelectMany(r=>store.Artifacts(r.RunId)),
                        approvals=store.Runs().Where(r=>r.TaskId==request.TaskId).SelectMany(r=>store.Approvals(r.RunId)),
                        observation="Primary milestones are reported by the external Codex; native tool telemetry is not collected."}
                };
                Console.WriteLine(JsonSerializer.Serialize(output,Json));return 0;
            }
            var workspaces=new ProjectWorkspace(settings.DataRoot);
            if(operation=="project") {
                var p=request.Project??throw new ArgumentException("Project required.");
                if(store.Tasks().Any(t=>t.WorkspaceId==p.ProjectId && t.Direct?.Stage!=DirectStage.Discussing && t.Lifecycle is not (TaskLifecycle.Done or TaskLifecycle.Cancelled or TaskLifecycle.Delivered)))
                    throw new InvalidOperationException("Project has unfinished tasks. Reconcile their agreements before changing project configuration.");
                var now=DateTimeOffset.UtcNow;
                p=workspaces.Validate(p with {ProjectId=string.IsNullOrWhiteSpace(p.ProjectId)?Guid.NewGuid().ToString("N"):p.ProjectId,
                    CreatedAt=p.CreatedAt==default?now:p.CreatedAt,UpdatedAt=now});
                store.SaveProject(p);Console.WriteLine(JsonSerializer.Serialize(p,Json));return 0;
            }
            // Native executables and auth are only used by verification, never by the observer.
            if(operation=="verify")settings.Validate();
            var factory=new RunHandlerFactory(settings,store);
            var service=new DirectWorkflowService(store,workspaces,factory,factory,new MicrosoftAgentFrameworkBackend(),new DirectDocuments(settings.DocumentsRoot));
            var token=cancellation.Token;
            var t=operation switch {
                "create"=>service.Create(request.ProjectId,request.Title,request.Risk,request.PrimaryLabel,request.NativeSessionRef,true),
                "agree"=>await service.Agree(request.TaskId,request.Revision,request.Anchors??store.Task(request.TaskId).Direct?.Documents?.Brief.Anchors??throw new ArgumentException("Record discussion documents first."),
                    request.Checks.IsDefaultOrEmpty?store.Task(request.TaskId).Direct?.Documents?.Brief.Checks??[]:request.Checks,request.Owner??throw new ArgumentException("Owner authorization required."),token),
                "document"=>await service.Document(request.TaskId,request.Revision,request.Brief??throw new ArgumentException("Brief required.")),
                "align"=>await service.Align(request.TaskId,request.Revision,request.Alignment??throw new ArgumentException("Alignment required."),token),
                "lens"=>await AttachLens(service,store,settings,request,token),
                "begin"=>await service.Begin(request.TaskId,request.Revision,request.Owner,token),
                "submit"=>await service.Submit(request.TaskId,request.Revision,request.Message,token),
                "verify"=>await service.Verify(request.TaskId,request.Revision,token),
                "feedback"=>await service.Feedback(request.TaskId,request.Revision,request.Message,false,request.Owner),
                "reopen"=>await service.Feedback(request.TaskId,request.Revision,request.Message,true,request.Owner),
                "accept"=>await service.Accept(request.TaskId,request.Revision,request.Owner??throw new ArgumentException("Owner acceptance required."),token),
                "recover"=>await service.Recover(request.TaskId,request.Revision,request.Owner??throw new ArgumentException("Recovery acknowledgement required.")),
                _=>throw new ArgumentException("Unknown direct operation: "+operation)
            };
            Console.WriteLine(JsonSerializer.Serialize(t,Json));
            return t.Direct?.Stage is DirectStage.Blocked or DirectStage.Rework?3:0;
        } catch(Exception ex) when(ex is ArgumentException or InvalidOperationException or IOException or UnauthorizedAccessException or JsonException or KeyNotFoundException or OperationCanceledException) {
            Console.Error.WriteLine(JsonSerializer.Serialize(new {error=JsonlEventSink.Redact(ex.Message),type=ex.GetType().Name},Json));return 2;
        } finally {Console.CancelKeyPress-=cancel;}
    }
}
