using Ares.Workbench.Domain;
namespace Ares.Workbench.Domain.Tests;
public class ContractTests
{
    private static NodeDefinition Node(string id) => new(id, NodeKind.Deterministic, "context/v1", "node-result/v1", SideEffect.ReadOnly, TimeSpan.FromSeconds(1), new(), id);
    [Fact] public void TaskContainsGoalNotExecutionState()
    {
        var names = typeof(EngineeringTask).GetProperties().Select(p => p.Name).ToArray();
        Assert.Contains("Acceptance", names);
        foreach (var name in new[] { "ReworkCount", "CurrentNode", "Attempt" }) Assert.DoesNotContain(name, names);
    }
    [Fact] public void DefinitionRejectsAmbiguousRoute()
    {
        var d = new WorkflowDefinition("flow", 1, Risk.Fast, "a", [Node("a")],
            [new("a",NodeOutcome.Succeeded,"$complete"),new("a",NodeOutcome.Succeeded,"a")], ["a"], new(3,"a"));
        Assert.Throws<ArgumentException>(d.Validate);
    }
    [Fact] public void DefinitionRejectsMissingTarget()
    {
        var d = new WorkflowDefinition("flow",1,Risk.Fast,"a",[Node("a")],[new("a",NodeOutcome.Succeeded,"missing")],["a"],new(3,"a"));
        Assert.Throws<ArgumentException>(d.Validate);
    }
    [Fact] public void InvalidOutputContractIsRejected() =>
        Assert.Throws<InvalidOperationException>(() => (NodeResult.Success() with { Contract="wrong/v1" }).Validate(Node("a")));
    [Fact] public void WaitingOnlyBelongsToHuman()
    {
        var result=new NodeResult(NodeOutcome.Waiting,"node-result/v1","",[],HumanRequest:new("id","scope",1,"reason"));
        Assert.Throws<InvalidOperationException>(() => result.Validate(Node("a")));
        result.Validate(Node("a") with { Kind=NodeKind.Human });
    }
    [Fact] public void FailureMustBeExplicit() =>
        Assert.Throws<InvalidOperationException>(() => new NodeResult(NodeOutcome.Failed,"node-result/v1","",[]).Validate(Node("a")));
    [Fact] public void ReworkNeedsActionableFeedback() =>
        Assert.Throws<InvalidOperationException>(() => NodeResult.Rework("").Validate(Node("a")));
}
