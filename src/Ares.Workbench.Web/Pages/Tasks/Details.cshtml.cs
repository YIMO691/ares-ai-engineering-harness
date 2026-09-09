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
    [BindProperty]public long Revision{get;set;}
    [BindProperty]public string Message{get;set;}="";
    [BindProperty]public string Goal{get;set;}="";
    [BindProperty]public string Acceptance{get;set;}="";
    [BindProperty]public string NonGoals{get;set;}="";
    [BindProperty]public string Boundary{get;set;}="";
    [BindProperty]public string KeyDecisions{get;set;}="";
    [BindProperty]public string Verification{get;set;}="";
    [BindProperty]public string Unresolved{get;set;}="";
    private static string[] Lines(string? value)=>(value??"").Split('\n',StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries);
    private ReadyAnchors Anchors()=>new(Goal,[..Lines(Acceptance)],NonGoals,Boundary,KeyDecisions,Verification,[..Lines(Unresolved)]);
    private void Load(string id,bool populate=false)
    {
        TaskItem=Store.Task(id);Project=Store.Project(TaskItem.WorkspaceId);
        History=Store.Tickets().Where(t=>t.TaskId==id).OrderByDescending(t=>t.CreatedAt).ToArray();
        if(populate&&TaskItem.Fusion is {} f) {
            Revision=f.Revision;var a=f.Draft;Goal=a.Goal;Acceptance=string.Join("\n",a.Acceptance);
            NonGoals=a.NonGoals;Boundary=a.Boundary;KeyDecisions=a.KeyDecisions;
            Verification=a.Verification;Unresolved=string.Join("\n",a.Unresolved);
        }
    }
    public IActionResult OnGet(string id){try{Load(id,true);return Page();}catch(KeyNotFoundException){return NotFound();}}
    private IActionResult Handle(string id,Action action)
    {
        try{action();return RedirectToPage(new{id});}
        catch(Exception ex) when(ex is InvalidOperationException or ArgumentException or UnauthorizedAccessException or IOException){
            Load(id);ModelState.AddModelError("",ex.Message);return Page();
        }
    }
    public IActionResult OnPostDiscuss(string id)=>Handle(id,()=>service.RequestDiscussion(id,Revision,Message));
    public IActionResult OnPostReopen(string id)=>Handle(id,()=>service.ReturnToDiscussion(id,Revision));
    public IActionResult OnPostStopDiscussion(string id)=>Handle(id,()=>service.StopDiscussion(id));
    public IActionResult OnPostSaveDraft(string id)=>Handle(id,()=>service.SaveReadyDraft(id,Revision,Anchors()));
    public IActionResult OnPostReady(string id)
    {
        try {
            service.FreezeReady(id,Revision,Anchors());
            var runId=service.RequestRun(id);return RedirectToPage("/Runs/Details",new{id=runId});
        } catch(Exception ex) when(ex is InvalidOperationException or ArgumentException or UnauthorizedAccessException or IOException){
            Load(id);ModelState.AddModelError("",ex.Message);return Page();
        }
    }
    public IActionResult OnPostAccept(string id,string runId,int readyVersion,bool accept,string comment)=>
        Handle(id,()=>service.OwnerAcceptance(id,runId,readyVersion,accept,comment??""));
    public IActionResult OnPostRun(string id)
    {
        try{var runId=service.RequestRun(id);return RedirectToPage("/Runs/Details",new{id=runId});}
        catch(Exception ex) when(ex is InvalidOperationException or ArgumentException or UnauthorizedAccessException or IOException){Load(id);ModelState.AddModelError("",ex.Message);return Page();}
    }
}
