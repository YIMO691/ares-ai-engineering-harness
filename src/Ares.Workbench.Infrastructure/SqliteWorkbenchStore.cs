using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ares.Workbench.Application;
using Ares.Workbench.Domain;
using Microsoft.Data.Sqlite;
namespace Ares.Workbench.Infrastructure;

/// <summary>SQLite implements the existing ports; JSON snapshots retain the immutable Domain contracts.</summary>
public sealed class SqliteWorkbenchStore : IWorkbenchStore
{
    private readonly object sync=new();
    private readonly string connectionString;
    private readonly string artifactRoot;
    public static readonly JsonSerializerOptions Json = new() { WriteIndented=true, Converters={new JsonStringEnumConverter()} };
    public SqliteWorkbenchStore(string dataRoot)
    {
        dataRoot=LocalPaths.Output(dataRoot);Directory.CreateDirectory(dataRoot);
        artifactRoot=LocalPaths.Output(Path.Combine(dataRoot,"artifacts"));Directory.CreateDirectory(artifactRoot);
        connectionString=new SqliteConnectionStringBuilder {DataSource=LocalPaths.Output(Path.Combine(dataRoot,"ares-workbench.db")),Pooling=false}.ToString();
        using var connection=Open();
        using var command=connection.CreateCommand();
        command.CommandText="""
            PRAGMA journal_mode=WAL;
            CREATE TABLE IF NOT EXISTS projects(id TEXT PRIMARY KEY, parent TEXT, json TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS tasks(id TEXT PRIMARY KEY, parent TEXT NOT NULL, json TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS runs(id TEXT PRIMARY KEY, parent TEXT NOT NULL, json TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS events(id TEXT PRIMARY KEY, parent TEXT NOT NULL, json TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS artifacts(id TEXT PRIMARY KEY, parent TEXT NOT NULL, json TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS approvals(id TEXT PRIMARY KEY, parent TEXT NOT NULL, json TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS tickets(id TEXT PRIMARY KEY, parent TEXT NOT NULL, json TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS checkpoints(id TEXT PRIMARY KEY, parent TEXT NOT NULL, json TEXT NOT NULL);
            CREATE INDEX IF NOT EXISTS ix_tasks_project ON tasks(parent);
            CREATE INDEX IF NOT EXISTS ix_runs_task ON runs(parent);
            CREATE INDEX IF NOT EXISTS ix_events_run ON events(parent);
            CREATE INDEX IF NOT EXISTS ix_artifacts_run ON artifacts(parent);
            CREATE INDEX IF NOT EXISTS ix_approvals_run ON approvals(parent);
            """;
        command.ExecuteNonQuery();
    }
    private SqliteConnection Open(){var c=new SqliteConnection(connectionString);c.Open();return c;}
    private List<T> List<T>(string table,string? parent=null)
    {
        lock(sync) {
            using var c=Open();using var cmd=c.CreateCommand();
            cmd.CommandText=$"SELECT json FROM {table}"+(parent is null?"":" WHERE parent=$parent")+" ORDER BY rowid";
            if(parent is not null)cmd.Parameters.AddWithValue("$parent",parent);
            using var reader=cmd.ExecuteReader();var values=new List<T>();
            while(reader.Read())values.Add(JsonSerializer.Deserialize<T>(reader.GetString(0),Json)??throw new InvalidDataException());
            return values;
        }
    }
    private T? Find<T>(string table,string id) where T:class
    {
        lock(sync) {
            using var c=Open();using var cmd=c.CreateCommand();cmd.CommandText=$"SELECT json FROM {table} WHERE id=$id";
            cmd.Parameters.AddWithValue("$id",id);var json=cmd.ExecuteScalar() as string;
            return json is null?null:JsonSerializer.Deserialize<T>(json,Json);
        }
    }
    private void Put<T>(string table,string id,string? parent,T value,bool insertOnly=false)
    {
        lock(sync) {
            using var c=Open();using var cmd=c.CreateCommand();
            cmd.CommandText=$"INSERT INTO {table}(id,parent,json) VALUES($id,$parent,$json)"+
                (insertOnly?"":" ON CONFLICT(id) DO UPDATE SET parent=excluded.parent,json=excluded.json");
            cmd.Parameters.AddWithValue("$id",id);cmd.Parameters.AddWithValue("$parent",(object?)parent??DBNull.Value);
            cmd.Parameters.AddWithValue("$json",JsonSerializer.Serialize(value,Json));cmd.ExecuteNonQuery();
        }
    }
    public IReadOnlyList<ProjectProfile> Projects()=>List<ProjectProfile>("projects");
    public ProjectProfile Project(string id)=>Find<ProjectProfile>("projects",id)??throw new KeyNotFoundException("Project not found");
    public void SaveProject(ProjectProfile value)=>Put("projects",value.ProjectId,null,value);
    public IReadOnlyList<EngineeringTask> Tasks()=>List<EngineeringTask>("tasks");
    public EngineeringTask Task(string id)=>Find<EngineeringTask>("tasks",id)??throw new KeyNotFoundException("Task not found");
    EngineeringTask ITaskStore.Get(string id)=>Task(id);
    void ITaskStore.Add(EngineeringTask value)=>Put("tasks",value.TaskId,value.WorkspaceId,value,true);
    void ITaskStore.Update(EngineeringTask value){_ = Task(value.TaskId);Put("tasks",value.TaskId,value.WorkspaceId,value);}
    public IReadOnlyList<WorkflowRun> Runs()=>List<WorkflowRun>("runs");
    public WorkflowRun? FindRun(string id)=>Find<WorkflowRun>("runs",id);
    WorkflowRun IRunStore.Get(string id)=>FindRun(id)??throw new KeyNotFoundException();
    void IRunStore.Add(WorkflowRun value)=>Put("runs",value.RunId,value.TaskId,value,true);
    void IRunStore.Update(WorkflowRun value)
    {
        lock(sync) {
            var old=FindRun(value.RunId)??throw new KeyNotFoundException();
            if(value.Version!=old.Version+1)throw new InvalidOperationException("Stale Run snapshot");
            Put("runs",value.RunId,value.TaskId,value);
            if(Checkpoint(value.RunId) is not null)UpdateCheckpoint(value.RunId,c=>c.WithRun(value));
            foreach(var path in value.Executions.SelectMany(e=>e.Result.ArtifactRefs).Distinct())
                Register(value.RunId,path);
        }
    }
    bool IRunStore.HasActiveRun(string taskId)=>Runs().Any(r=>r.TaskId==taskId&&r.State is RunState.Created or RunState.Running or RunState.Waiting);
    public IReadOnlyList<WorkbenchEvent> Events(string runId)=>List<WorkbenchEvent>("events",runId);
    public ValueTask AppendAsync(WorkbenchEvent value,CancellationToken cancellationToken=default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // Redact before persistence and before serving content through the UI.
        var clean=JsonSerializer.Deserialize<WorkbenchEvent>(JsonlEventSink.Redact(JsonSerializer.Serialize(value,Json)),Json)!;
        Put("events",value.EventId,value.RunId,clean,true);
        var dir=LocalPaths.Output(Path.Combine(artifactRoot,value.RunId));Directory.CreateDirectory(dir);
        var path=LocalPaths.Output(Path.Combine(dir,"events.jsonl"));
        lock(sync)File.AppendAllText(path,JsonSerializer.Serialize(clean)+Environment.NewLine);
        Register(value.RunId,path);return ValueTask.CompletedTask;
    }
    private void Register(string runId,string path)
    {
        var root=LocalPaths.Output(Path.Combine(artifactRoot,runId));
        var full=WorkspacePolicy.Under(root,Path.GetFullPath(path));
        if(!File.Exists(full))return;
        var relative=Path.GetRelativePath(root,full).Replace('\\','/');
        var id=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(runId+":"+relative)));
        SaveArtifact(new(id,runId,Path.GetExtension(relative).TrimStart('.'),relative,Path.GetFileName(relative),DateTimeOffset.UtcNow));
    }
    public IReadOnlyList<ArtifactEntry> Artifacts(string runId)=>List<ArtifactEntry>("artifacts",runId);
    public void SaveArtifact(ArtifactEntry value)=>Put("artifacts",value.ArtifactId,value.RunId,value);
    public IReadOnlyList<ApprovalEntry> Approvals(string runId)=>List<ApprovalEntry>("approvals",runId);
    public void SaveApproval(ApprovalEntry value)=>Put("approvals",value.ApprovalId,value.RunId,value);
    public IReadOnlyList<RunTicket> Tickets()=>List<RunTicket>("tickets");
    public RunTicket Ticket(string id)=>Find<RunTicket>("tickets",id)??throw new KeyNotFoundException("Run request not found");
    public void SaveTicket(RunTicket value)=>Put("tickets",value.RunId,value.TaskId,value);
    public RunCheckpoint? Checkpoint(string id)=>Find<RunCheckpoint>("checkpoints",id);
    public void SaveCheckpoint(RunCheckpoint value)=>Put("checkpoints",value.RunId,value.RunId,value);
    public void UpdateCheckpoint(string id,Func<RunCheckpoint,RunCheckpoint> update) {
        lock(sync) { var current=Checkpoint(id)??throw new KeyNotFoundException("Checkpoint unavailable");
            SaveCheckpoint(update(current) with {UpdatedAt=DateTimeOffset.UtcNow}); }
    }
}
