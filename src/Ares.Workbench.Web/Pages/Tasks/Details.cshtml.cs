using Ares.Workbench.Application;
using Ares.Workbench.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace Ares.Workbench.Web.Pages.Tasks;
public class DetailsModel(WorkbenchService service):PageModel
{
    public EngineeringTask TaskItem{get;set;}=null!;
    public ProjectProfile Project{get;set;}=null!;
    public IReadOnlyList<RunTicket> History{get;set;}=[];
    public IWorkbenchStore Store=>service.Read;
    private void Load(string id){TaskItem=Store.Task(id);Project=Store.Project(TaskItem.WorkspaceId);History=Store.Tickets().Where(t=>t.TaskId==id).OrderByDescending(t=>t.CreatedAt).ToArray();}
    public IActionResult OnGet(string id){try{Load(id);return Page();}catch(KeyNotFoundException){return NotFound();}}
    public IActionResult OnPostRun(string id)
    {
        try{var runId=service.RequestRun(id);return RedirectToPage("/Runs/Details",new{id=runId});}
        catch(Exception ex) when(ex is InvalidOperationException or ArgumentException or UnauthorizedAccessException or IOException){Load(id);ModelState.AddModelError("",ex.Message);return Page();}
    }
}
