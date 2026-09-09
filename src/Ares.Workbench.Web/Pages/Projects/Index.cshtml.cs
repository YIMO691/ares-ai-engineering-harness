using Ares.Workbench.Application;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace Ares.Workbench.Web.Pages.Projects;
public class IndexModel(WorkbenchService service):PageModel{public IReadOnlyList<ProjectProfile> Projects=>service.Read.Projects();}
