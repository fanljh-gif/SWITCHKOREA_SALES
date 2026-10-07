using SalesManagement.Core.Models;
using SalesManagement.Core.Repositories;
using SalesManagement.Core.Research;

namespace SalesManagement.Api.Workers;

public sealed class ResearchWorker(IServiceScopeFactory scopeFactory, ILogger<ResearchWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var runs = scope.ServiceProvider.GetRequiredService<IResearchRunRepository>();
                var service = scope.ServiceProvider.GetRequiredService<ResearchRunService>();
                foreach (var run in (await runs.ListAsync(stoppingToken)).Where(x => x.Status == ResearchRunStatus.Queued).Take(1)) await service.ProcessAsync(run.Id, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception exception) { logger.LogError(exception, "Prospect research worker iteration failed."); }
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
}
