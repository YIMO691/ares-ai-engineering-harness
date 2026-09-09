using Ares.Workbench.Application;
using Ares.Workbench.Domain;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace Ares.Workbench.Web.Pages.Tasks;
public class IndexModel(WorkbenchService service):PageModel
{
    public IWorkbenchStore Store=>service.Read;
    public string Filter{get;set;}="All";
    public void OnGet(string? filter)=>Filter=filter??"All";
    public IReadOnlyList<EngineeringTask> Items()=>Store.Tasks().Where(t=>Filter switch {
        "Delivered"=>t.Lifecycle==TaskLifecycle.Delivered,
        "Open"=>t.Lifecycle is TaskLifecycle.Draft or TaskLifecycle.Active,
        "Running"=>Store.Runs().Any(r=>r.TaskId==t.TaskId&&r.State==RunState.Running),
        "Blocked"=>Store.Runs().Where(r=>r.TaskId==t.TaskId).OrderByDescending(r=>r.StartedAt).FirstOrDefault()?.State is RunState.Blocked or RunState.Failed or RunState.Paused,
        _=>true}).OrderByDescending(t=>t.CreatedAt).ToArray();
}
