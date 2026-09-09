using Ares.Workbench.Application;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace Ares.Workbench.Web.Pages;
public class IndexModel(WorkbenchService service):PageModel
{
    public IWorkbenchStore Store=>service.Read;
}
