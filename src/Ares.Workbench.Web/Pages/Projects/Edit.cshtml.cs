using System.Collections.Immutable;
using Ares.Workbench.Application;
using Ares.Workbench.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace Ares.Workbench.Web.Pages.Projects;
public class EditModel(WorkbenchService service):PageModel
{
    [BindProperty] public string ProjectId{get;set;}="";
    [BindProperty] public string Name{get;set;}="";
    [BindProperty] public string RepoRoot{get;set;}="";
    [BindProperty] public Risk DefaultRisk{get;set;}=Risk.Standard;
    [BindProperty] public string BuildCommand{get;set;}="dotnet build";
    [BindProperty] public string TestCommand{get;set;}="dotnet test";
    [BindProperty] public string AllowedPaths{get;set;}=".";
    [BindProperty] public string InstructionFiles{get;set;}="AGENTS.md";
    public string? Warning{get;set;}
    public IActionResult OnGet(string? id)
    {
        if(id is null)return Page();
        try{var p=service.Read.Project(id);ProjectId=p.ProjectId;Name=p.Name;RepoRoot=p.RepoRoot;DefaultRisk=p.DefaultRisk;BuildCommand=p.BuildCommand;TestCommand=p.TestCommand;AllowedPaths=string.Join("\n",p.AllowedPaths);InstructionFiles=string.Join("\n",p.InstructionFiles);return Page();}
        catch(KeyNotFoundException){return NotFound();}
    }
    public IActionResult OnPost()
    {
        if(!ModelState.IsValid)return Page();
        try{
            var now=DateTimeOffset.UtcNow;
            var id=string.IsNullOrWhiteSpace(ProjectId)?Guid.NewGuid().ToString("N"):ProjectId;
            var created=string.IsNullOrWhiteSpace(ProjectId)?now:service.Read.Project(id).CreatedAt;
            var p=service.SaveProject(new(id,Name,RepoRoot,DefaultRisk,BuildCommand,TestCommand,Lines(AllowedPaths),Lines(InstructionFiles),created,now));
            if(!Directory.Exists(Path.Combine(p.RepoRoot,".git"))&&!System.IO.File.Exists(Path.Combine(p.RepoRoot,".git")))
                TempData["Notice"]="配置已保存，但目录未发现 .git。运行前请初始化 Git 仓库。";
            return RedirectToPage("/Projects/Index");
        }catch(Exception ex) when(ex is ArgumentException or InvalidOperationException or UnauthorizedAccessException or IOException or KeyNotFoundException){ModelState.AddModelError("",ex.Message);return Page();}
    }
    private static ImmutableArray<string> Lines(string? text)=>(text??"").Split('\n',StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries).ToImmutableArray();
}
