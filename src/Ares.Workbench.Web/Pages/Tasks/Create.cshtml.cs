using Ares.Workbench.Application;
using Ares.Workbench.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace Ares.Workbench.Web.Pages.Tasks;
public class CreateModel(WorkbenchService service):PageModel
{
    public IReadOnlyList<ProjectProfile> Projects=>service.Read.Projects();
    [BindProperty]public string ProjectId{get;set;}="";
    [BindProperty]public string Title{get;set;}="";
    [BindProperty]public string Goal{get;set;}="";
    [BindProperty]public string Acceptance{get;set;}="";
    [BindProperty]public string Constraints{get;set;}="";
    [BindProperty]public Risk Risk{get;set;}=Risk.Standard;
    public void OnGet(string? projectId)
    {
        ProjectId=projectId??Projects.FirstOrDefault()?.ProjectId??"";
        if(Projects.FirstOrDefault(p=>p.ProjectId==ProjectId) is {} p)Risk=p.DefaultRisk;
    }
    public IActionResult OnPost()
    {
        if(!ModelState.IsValid)return Page();
        try{var task=service.CreateDiscussingTask(ProjectId,Title,Goal,Lines(Acceptance),Lines(Constraints),Risk);return RedirectToPage("/Tasks/Details",new{id=task.TaskId});}
        catch(Exception ex) when(ex is ArgumentException or KeyNotFoundException){ModelState.AddModelError("",ex.Message);return Page();}
    }
    private static string[] Lines(string? value)=>(value??"").Split('\n',StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries);
}
