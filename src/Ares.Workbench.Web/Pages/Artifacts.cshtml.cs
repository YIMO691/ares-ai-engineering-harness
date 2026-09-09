using Ares.Workbench.Application;
using Ares.Workbench.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace Ares.Workbench.Web.Pages;
public class ArtifactsModel(WorkbenchService service,RuntimeSettings settings):PageModel
{
    public IActionResult OnGet(string runId,string artifactId)
    {
        var item=service.Read.Artifacts(runId).FirstOrDefault(a=>a.ArtifactId==artifactId);
        if(item is null)return NotFound();
        try {
            var root=LocalPaths.Output(Path.Combine(settings.DataRoot,"artifacts",runId));
            var path=WorkspacePolicy.Under(root,Path.Combine(root,item.RelativePath));
            if(!System.IO.File.Exists(path))return NotFound();
            return File(System.IO.File.ReadAllBytes(path),"text/plain; charset=utf-8");
        }catch(UnauthorizedAccessException){return BadRequest();}
    }
}
