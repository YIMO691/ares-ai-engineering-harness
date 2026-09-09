using System.Collections.Concurrent;
using Ares.Workbench.Application;
using Ares.Workbench.Domain;
namespace Ares.Workbench.Infrastructure;
public sealed class InMemoryTaskStore : ITaskStore
{
    private readonly ConcurrentDictionary<string,EngineeringTask> data=[];
    public EngineeringTask Get(string id)=>data[id];
    public void Add(EngineeringTask task){if(!data.TryAdd(task.TaskId,task))throw new InvalidOperationException("Duplicate task.");}
    public void Update(EngineeringTask task){if(!data.ContainsKey(task.TaskId))throw new KeyNotFoundException();data[task.TaskId]=task;}
}
public sealed class InMemoryRunStore : IRunStore
{
    private readonly ConcurrentDictionary<string,WorkflowRun> data=[];
    public WorkflowRun Get(string id)=>data[id];
    public void Add(WorkflowRun run){if(!data.TryAdd(run.RunId,run))throw new InvalidOperationException("Duplicate run.");}
    public void Update(WorkflowRun run){if(!data.ContainsKey(run.RunId))throw new KeyNotFoundException();data[run.RunId]=run;}
    public bool HasActiveRun(string taskId)=>data.Values.Any(r=>r.TaskId==taskId&&r.State is RunState.Created or RunState.Running or RunState.Waiting or RunState.Blocked);
}
