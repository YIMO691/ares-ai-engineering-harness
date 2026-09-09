using Ares.Workbench.Domain;
namespace Ares.Workbench.Web;
public static class UiText
{
    public static string State(RunState s)=>s switch {
        RunState.Created=>"已创建",RunState.Running=>"运行中",RunState.Waiting=>"等待审批",RunState.Completed=>"已完成",
        RunState.Paused=>"Paused · 已暂停",RunState.Blocked=>"Blocked · 需要处理",RunState.Cancelled=>"已取消",_=>"失败"};
    public static string Task(TaskLifecycle s)=>s switch {TaskLifecycle.Draft=>"待运行",TaskLifecycle.Active=>"进行中",TaskLifecycle.Delivered=>"已交付",_=>"已取消"};
    public static string Node(string? id)=>id switch {"prepare"=>"Primary Codex · 理解与准备","grounding"=>"Grounding · 项目理解","plan"=>"Plan · 实施计划","codex"=>"Primary Codex · 实现 / 返工","test"=>"Test · 确定性验证","review"=>"Reviewer · 验收审查","human"=>"Approval · Owner 审批","deliver"=>"Deliver · 交付",_=>id??"—"};
}
