using Ares.Workbench.Application;
using Ares.Workbench.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace Ares.Workbench.Web.Pages;
public class TaskFileModel(IWorkbenchStore store,RuntimeSettings settings):PageModel {
    public IActionResult OnGet(string id,string role,bool lens=false) {
        try {
            var d=store.Task(id).Direct;if(d is null)return NotFound();
            var f=(lens?d.Lens?.Files:d.Documents?.Files)?.FirstOrDefault(x=>x.Role==role);if(f is null)return NotFound();
            if(lens&&(d.Lens!.RunId!=d.LastRunId||d.Lens.SourceStamp!=d.SubmittedStamp))return StatusCode(409);
            var root=lens?Path.Combine(settings.DataRoot,"artifacts",d.Lens!.RunId,"lens"):settings.DocumentsRoot;
            var path=WorkspacePolicy.Under(LocalPaths.Output(root),f.Path);
            if(!System.IO.File.Exists(path))return NotFound();
            var content=System.IO.File.ReadAllText(path);
            if(DirectDocuments.Hash(content)!=f.Sha256)return StatusCode(409,"文件已变化，需重新登记或生成。");
            Response.Headers["Content-Security-Policy"]="default-src 'none'; style-src 'unsafe-inline'; img-src data:; sandbox";
            Response.Headers["X-Content-Type-Options"]="nosniff";
            return Content(content,lens&&role=="change-story.html"?"text/html; charset=utf-8":"text/plain; charset=utf-8");
        }catch(KeyNotFoundException){return NotFound();}
        catch(UnauthorizedAccessException){return BadRequest();}
    }
}
