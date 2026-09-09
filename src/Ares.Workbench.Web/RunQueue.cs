using Ares.Workbench.Adapters.Codex;
using System.Threading.Channels;
using Ares.Workbench.Application;
namespace Ares.Workbench.Web;
public sealed class RunQueue : IWorkbenchQueue
{
    private readonly Channel<QueueItem> channel=Channel.CreateUnbounded<QueueItem>(new UnboundedChannelOptions{SingleReader=true,SingleWriter=false});
    public ChannelReader<QueueItem> Reader=>channel.Reader;
    public void Enqueue(QueueItem item){if(!channel.Writer.TryWrite(item))throw new InvalidOperationException("Run queue unavailable");}
}
public sealed class RunWorker(RunQueue queue,WorkbenchService service,ILogger<RunWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach(var item in queue.Reader.ReadAllAsync(stoppingToken)) {
            try{await service.ProcessAsync(item,stoppingToken);}
            catch(OperationCanceledException) when(stoppingToken.IsCancellationRequested){break;}
            catch(Exception ex){logger.LogError(ex,"Run {RunId} failed",item.RunId);}
        }
    }
}
public sealed class CodexAuthentication(RuntimeSettings settings) : IHostedService
{
    private string? copy;
    public Task StartAsync(CancellationToken ct)
    {
        string source=string.IsNullOrWhiteSpace(settings.AuthSource)?
            Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile),".codex","auth.json"):settings.AuthSource;
        var target=Ares.Workbench.Infrastructure.LocalPaths.Output(Path.Combine(settings.CodexHome,"auth.json"));
        if(!Path.GetFullPath(source).Equals(target,StringComparison.OrdinalIgnoreCase) && File.Exists(source)) {
            File.Copy(source,target,true);copy=target;
        }
        return Task.CompletedTask;
    }
    public Task StopAsync(CancellationToken ct)
    {
        if(copy is not null&&File.Exists(copy))File.Delete(Ares.Workbench.Infrastructure.LocalPaths.Output(copy));
        return Task.CompletedTask;
    }
}
