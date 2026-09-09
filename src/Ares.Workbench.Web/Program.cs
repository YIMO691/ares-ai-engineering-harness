using Ares.Workbench.Adapters.Codex;
using Ares.Workbench.Application;
using Ares.Workbench.Adapters.AgentFramework;
using Ares.Workbench.Infrastructure;
using Ares.Workbench.Web;
using Microsoft.AspNetCore.DataProtection;

var builder=WebApplication.CreateBuilder(args);
var settingsFile=Environment.GetEnvironmentVariable("ARES_SETTINGS_FILE");
if(!string.IsNullOrWhiteSpace(settingsFile))builder.Configuration.AddJsonFile(Path.GetFullPath(settingsFile),optional:false,reloadOnChange:false).AddEnvironmentVariables();
var settings=builder.Configuration.GetSection("Workbench").Get<RuntimeSettings>()??new();
if(settings.ObserverOnly)settings.ValidateObserver();else settings.Validate();
using var instanceLock=settings.ObserverOnly?null:new FileStream(LocalPaths.Output(Path.Combine(settings.DataRoot,".writer.lock")),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
if(string.IsNullOrWhiteSpace(builder.Configuration["urls"]))builder.WebHost.UseUrls("http://127.0.0.1:5271");
builder.Services.AddSingleton(settings);
builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(LocalPaths.Output(Path.Combine(settings.DataRoot,"keys"))));
builder.Services.AddRazorPages().AddMvcOptions(o=>o.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes=true);
builder.Services.AddSingleton<IWorkbenchStore>(_=>new SqliteWorkbenchStore(settings.DataRoot));
builder.Services.AddSingleton<IProjectWorkspace>(_=>new ProjectWorkspace(settings.DataRoot));
builder.Services.AddSingleton<IRunHandlerFactory,RunHandlerFactory>();
builder.Services.AddSingleton<IWorkflowBackend,MicrosoftAgentFrameworkBackend>();
builder.Services.AddSingleton<RunQueue>();
builder.Services.AddSingleton<IWorkbenchQueue>(sp=>sp.GetRequiredService<RunQueue>());
builder.Services.AddSingleton<WorkbenchService>();
if(!settings.ObserverOnly) {
    builder.Services.AddHostedService<CodexAuthentication>();
    builder.Services.AddHostedService<RunWorker>();
}
var app=builder.Build();
app.Use(async(context,next)=>{
    if(context.Request.Host.Host is not ("127.0.0.1" or "localhost" or "::1") ||
        context.Connection.RemoteIpAddress is {} address && !System.Net.IPAddress.IsLoopback(address)) {
        context.Response.StatusCode=403;return;
    }
    if(settings.ObserverOnly && context.Request.Path=="/") {context.Response.Redirect("/Observe");return;}
    if(settings.ObserverOnly && context.Request.Method is not ("GET" or "HEAD")) {
        context.Response.StatusCode=405;await context.Response.WriteAsync("Observer is read-only. Continue in the original Codex conversation.");return;
    }
    if(settings.ObserverOnly && (context.Request.Path.StartsWithSegments("/Tasks/Create") || context.Request.Path.StartsWithSegments("/Projects/Edit"))) {
        context.Response.Redirect("/Observe");return;
    }
    context.Response.Headers["X-Content-Type-Options"]="nosniff";
    await next();
});
app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();
app.MapGet("/health",()=>Results.Json(new{status="ready",product="Ares Workbench",version="Codex Direct + Observer",observer_only=settings.ObserverOnly}));
if(!settings.ObserverOnly)await app.Services.GetRequiredService<WorkbenchService>().RecoverInterruptedAsync();
await app.RunAsync();
public partial class Program;
