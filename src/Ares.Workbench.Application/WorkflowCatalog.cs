using System.Collections.Immutable;
using Ares.Workbench.Domain;
namespace Ares.Workbench.Application;

public static class RiskRouter
{
    public static Risk Select(Risk requested, IEnumerable<string> constraints) =>
        constraints.Any(x => x.Contains("high-risk",StringComparison.OrdinalIgnoreCase) ||
                             x.Contains("irreversible",StringComparison.OrdinalIgnoreCase))
            ? Risk.Critical : requested;
}
public static class WorkflowCatalog
{
    public static WorkflowDefinition Create(Risk risk)
    {
        var sequence = risk switch
        {
            Risk.Fast => new[] {"codex","test","diff","deliver"},
            Risk.Standard => ["grounding","plan","codex","test","review","deliver"],
            Risk.Critical => ["grounding","architecture","plan","codex","test","independent-verify","review","human","deliver"],
            _ => throw new ArgumentOutOfRangeException(nameof(risk))
        };
        var nodes = sequence.Select(id => new NodeDefinition(id,id switch {
            "codex" => NodeKind.Codex, "plan" or "review" or "architecture" => NodeKind.Agent,
            "human" => NodeKind.Human, _ => NodeKind.Deterministic },
            "context/v1","node-result/v1",id=="codex"?SideEffect.WorkspaceWrite:SideEffect.ReadOnly,
            TimeSpan.FromMinutes(id=="codex"?8:3),new(0),id,
            id is "plan" or "review" or "architecture" ? id : null)).ToImmutableArray();
        var routes=sequence.Select((id,index) => new Route(id,NodeOutcome.Succeeded,
            index+1<sequence.Length?sequence[index+1]:"$complete")).ToList();
        foreach(var id in sequence.Where(x => x is "review" or "independent-verify" or "test"))
            if(risk != Risk.Fast) routes.Add(new(id,NodeOutcome.ReworkRequired,"codex"));
        var d=new WorkflowDefinition(risk.ToString().ToUpperInvariant()+"-v1",1,risk,sequence[0],
            nodes,[..routes],[..sequence],new(3,"codex"));
        d.Validate();return d;
    }
}
public sealed class TaskService(ITaskStore tasks)
{
    public EngineeringTask Create(string id,string title,string goal,IEnumerable<string> acceptance,
        IEnumerable<string> constraints,Risk requested,Workspace workspace)
    {
        var a=acceptance.ToImmutableArray();var c=constraints.ToImmutableArray();
        if(string.IsNullOrWhiteSpace(id)||string.IsNullOrWhiteSpace(goal)||a.IsEmpty)
            throw new ArgumentException("Task needs an id, goal and acceptance.");
        var now=DateTimeOffset.UtcNow;
        var task=new EngineeringTask(id,title,goal,a,c,RiskRouter.Select(requested,c),
            workspace.WorkspaceId,TaskLifecycle.Draft,now,now);
        tasks.Add(task);return task;
    }
}
