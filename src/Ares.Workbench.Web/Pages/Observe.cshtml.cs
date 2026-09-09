using Ares.Workbench.Application;
using Ares.Workbench.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace Ares.Workbench.Web.Pages;

public class ObserveModel(IWorkbenchStore store):PageModel {
    public IWorkbenchStore Store=>store;
    public EngineeringTask? TaskItem {get;private set;}
    public IReadOnlyList<WorkflowRun> Runs {get;private set;}=[];
    public IReadOnlyList<WorkbenchEvent> Timeline {get;private set;}=[];
    public IActionResult OnGet(string? id) {
        if(id is null)return Page();
        try {
            TaskItem=store.Task(id);
            Runs=store.Runs().Where(r=>r.TaskId==id).OrderByDescending(r=>r.StartedAt).ToArray();
            Timeline=store.Events(id).Concat(Runs.SelectMany(r=>store.Events(r.RunId))).OrderBy(e=>e.OccurredAt).ToArray();
            return Page();
        } catch(KeyNotFoundException){return NotFound();}
    }
    public static string Stage(DirectStage s)=>s switch {
        DirectStage.Discussing=>"讨论中",DirectStage.Ready=>"约定已确认",DirectStage.Implementing=>"Primary 实施中",
        DirectStage.Submitted=>"已提交验证",DirectStage.Checking=>"验证与审查中",DirectStage.Rework=>"等待 Primary 修复",
        DirectStage.Blocked=>"需要处理阻塞",DirectStage.AwaitingAcceptance=>"等待你验收",DirectStage.Done=>"已完成",_=>s.ToString()
    };
}
