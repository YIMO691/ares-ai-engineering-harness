using System.Collections.Immutable;
using Ares.Workbench.Domain;
namespace Ares.Workbench.Application;

/// <summary>MVP definitions run on the existing coordinator and AF backend; Phase 1 definitions remain available.</summary>
public static class MvpWorkflowCatalog
{
    public static WorkflowDefinition Create(Risk risk)
    {
        string[] sequence = risk switch {
            Risk.Fast => ["codex", "test", "deliver"],
            Risk.Standard => ["grounding", "plan", "codex", "test", "review", "deliver"],
            Risk.Critical => ["grounding", "plan", "codex", "test", "review", "human", "deliver"],
            _ => throw new ArgumentOutOfRangeException(nameof(risk))
        };
        var nodes = sequence.Select(id => new NodeDefinition(id, id switch {
            "grounding" or "plan" or "review" => NodeKind.Agent,
            "codex" => NodeKind.Codex, "human" => NodeKind.Human, _ => NodeKind.Deterministic
        }, "context/v1", "node-result/v1", id == "codex" ? SideEffect.WorkspaceWrite : SideEffect.ReadOnly,
            TimeSpan.FromMinutes(id is "test" or "deliver" or "human" ? 3 : 10), new(0), id,
            id is "grounding" or "plan" or "review" ? id : null)).ToImmutableArray();
        var routes = sequence.Select((id,n) => new Route(id,NodeOutcome.Succeeded,n+1<sequence.Length?sequence[n+1]:"$complete")).ToList();
        if(risk != Risk.Fast) routes.Add(new("review",NodeOutcome.ReworkRequired,"codex"));
        var definition = new WorkflowDefinition(risk.ToString().ToUpperInvariant()+"-MVP-v1",1,risk,sequence[0],
            nodes,[..routes],[..sequence],new(2,"codex"));
        definition.Validate();
        return definition;
    }
    public static WorkflowDefinition CreateV02(Risk risk)
    {
        string[] sequence = risk switch {
            Risk.Fast => ["codex", "test", "deliver"],
            Risk.Standard => ["prepare", "codex", "test", "review", "deliver"],
            Risk.Critical => ["prepare", "codex", "test", "review", "human", "deliver"],
            _ => throw new ArgumentOutOfRangeException(nameof(risk))
        };
        var nodes = sequence.Select(id => new NodeDefinition(id, id switch {
            "prepare" or "review" => NodeKind.Agent,
            "codex" => NodeKind.Codex, "human" => NodeKind.Human, _ => NodeKind.Deterministic
        }, "context/v1", "node-result/v1", id == "codex" ? SideEffect.WorkspaceWrite : SideEffect.ReadOnly,
            TimeSpan.FromMinutes(id is "test" or "deliver" or "human" ? 3 : 10), new(0), id,
            id is "prepare" or "review" ? id : null)).ToImmutableArray();
        var routes = sequence.Select((id,n) => new Route(id,NodeOutcome.Succeeded,n+1<sequence.Length?sequence[n+1]:"$complete")).ToList();
        if(risk != Risk.Fast) routes.Add(new("review",NodeOutcome.ReworkRequired,"codex"));
        var definition = new WorkflowDefinition(risk.ToString().ToUpperInvariant()+"-THIN-v2",2,risk,sequence[0],
            nodes,[..routes],[..sequence],new(2,"codex"));
        definition.Validate();
        return definition;
    }
}
