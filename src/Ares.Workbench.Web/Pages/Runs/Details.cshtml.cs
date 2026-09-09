using Ares.Workbench.Application;
using Ares.Workbench.Domain;
using Ares.Workbench.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace Ares.Workbench.Web.Pages.Runs;
public class DetailsModel(WorkbenchService service,RuntimeSettings settings):PageModel
{
    public RunTicket Ticket{get;set;}=null!;
    public EngineeringTask TaskItem{get;set;}=null!;
    public WorkflowRun? Run{get;set;}
    public IWorkbenchStore Store=>service.Read;
    [BindProperty]public string RequestId{get;set;}="";
    [BindProperty]public long RunVersion{get;set;}
    [BindProperty]public bool Approve{get;set;}
    [BindProperty]public string Comment{get;set;}="";
    private void Load(string id){Ticket=Store.Ticket(id);TaskItem=Store.Task(Ticket.TaskId);Run=Store.FindRun(id);}
    public IActionResult OnGet(string id){try{Load(id);return Page();}catch(KeyNotFoundException){return NotFound();}}
    private string Prepared(string id,string section) {
        if(Run?.Executions.Any(e=>e.NodeId=="prepare"&&e.Result.Outcome==NodeOutcome.Succeeded)!=true)return "";
        var entry=Store.Artifacts(id).FirstOrDefault(a=>a.RelativePath==section+".md");
        if(entry is null)return "";
        var root=LocalPaths.Output(Path.Combine(settings.DataRoot,"artifacts",id));
        var path=WorkspacePolicy.Under(root,Path.Combine(root,entry.RelativePath));
        return System.IO.File.Exists(path)?System.IO.File.ReadAllText(path):"已保存的输出文件缺失。";
    }
    public IActionResult OnGetSnapshot(string id)
    {
        try {
            Load(id);var run=Run;var cp=Store.Checkpoint(id);
            var running=run?.State==RunState.Running;
            return new JsonResult(new{
                state=run is null?(Ticket.State=="Queued"?"已排队":Ticket.State):UiText.State(run.State),
                rawState=run?.State.ToString()??Ticket.State,current=UiText.Node(run?.CurrentNode),currentNode=run?.CurrentNode,
                rework=run?.ReworkCount??0,
                lifecycle=run?.State is RunState.Blocked or RunState.Paused?"Needs Attention · 需要处理":UiText.Task(TaskItem.Lifecycle),
                failure=run?.Failure?.Message??Ticket.Error,
                workflow=run?.WorkflowId??TaskItem.Risk.ToString().ToUpperInvariant()+"-THIN-v2",
                nodes=(run?.Executions??[]).Select(e=>new{id=e.NodeId,attempt=e.Attempt,outcome=e.Result.Outcome.ToString(),output=e.Result.Output,failure=e.Result.Failure?.Message}),
                grounding=Prepared(id,"grounding"),plan=Prepared(id,"plan"),
                timeline=Store.Events(id).Where(e=>e.EventType!="BackendEvent").DistinctBy(e=>e.EventId)
                    .Select(e=>new{id=e.EventId,type=e.EventType,node=UiText.Node(e.NodeId),time=e.OccurredAt.LocalDateTime.ToString("HH:mm:ss"),detail=string.Join(" · ",e.Payload.Select(p=>p.Key+": "+p.Value))}),
                artifacts=Store.Artifacts(id).Select(a=>new{name=a.DisplayName,url="/Artifacts/"+id+"/"+a.ArtifactId}),
                pending=run?.PendingHuman is {} p?new{requestId=p.RequestId,version=p.RunVersion,reason=p.Reason}:null,
                approvals=Store.Approvals(id).Select(a=>new{a.Status,a.Comment}),
                controls=new{stop=running,cancel=running||run?.State is RunState.Paused or RunState.Blocked or RunState.Waiting||run is null&&Ticket.State=="Queued",resume=service.CanResume(id)},
                checkpoint=cp,
                primaryStatus=running&&run?.CurrentNode is "prepare" or "codex"?"Active":"Idle",
                reviewerStatus=running&&run?.CurrentNode=="review"?"Active":"Idle",
                blockedAt=UiText.Node(run?.CurrentNode),resumeTarget=UiText.Node(cp?.ResumeTarget),
                blockedCategory=cp?.BlockedCategory??(run?.Failure is null?"":RunCheckpoint.Category(run.Failure.Code)),
                ownerFix=run?.Failure?.Code switch {
                    "PROJECT_POLICY"=>"编辑 Project 的 allowed_paths，加入任务所需路径；指令文件始终只读。",
                    "PROJECT_CONFIGURATION"=>"检查 Project 配置、路径和可执行命令。",
                    "VERIFICATION_FAILED"=>"查看构建 / 测试输出，修复命令或环境后继续。",
                    "STOPPED"=>"确认当前源码和已保存结果后 Resume；不会自动回滚。",
                    "REWORK_BUDGET" or "HUMAN_REJECTED"=>"需要 Owner 决定后新建 Run。",
                    _=>cp is null||cp.ResumeSupported==false?"此历史记录不能 Resume，请 New Run。":"查看错误和执行证据，修复环境或补充输入后 Resume。"
                }
            });
        }catch(KeyNotFoundException){return NotFound();}
    }
    private IActionResult Mutate(string id,Action action) {
        try{action();return RedirectToPage(new{id});}
        catch(Exception ex) when(ex is InvalidOperationException or ArgumentException or UnauthorizedAccessException) {
            Load(id);ModelState.AddModelError("",ex.Message);return Page();
        }
    }
    public IActionResult OnPostStop(string id)=>Mutate(id,()=>service.RequestStop(id,false));
    public IActionResult OnPostCancel(string id)=>Mutate(id,()=>service.RequestStop(id,true));
    public IActionResult OnPostResume(string id)=>Mutate(id,()=>service.RequestResume(id));
    public IActionResult OnPostApproval(string id)=>Mutate(id,()=>service.RequestApproval(id,RequestId,RunVersion,Approve,Comment));
}
