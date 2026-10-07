using Microsoft.Extensions.Options;
using SalesManagement.Api.Research;
using SalesManagement.Core.Models;
using SalesManagement.Core.Repositories;
using SalesManagement.Core.Research;

namespace SalesManagement.Api.Workers;

public sealed class WeeklyResearchWorker(IServiceScopeFactory scopeFactory, IOptions<ResearchProviderOptions> options, TimeProvider timeProvider, ILogger<WeeklyResearchWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = NextDelay(timeProvider.GetUtcNow(), options.Value.WeeklyDay, options.Value.WeeklyTime);
            await Task.Delay(delay, stoppingToken);
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var profiles = scope.ServiceProvider.GetRequiredService<IResearchTargetProfileRepository>();
                var runs = scope.ServiceProvider.GetRequiredService<IResearchRunRepository>();
                var usage = new UsageSnapshot((await runs.ListAsync(stoppingToken)).Where(x => x.CreatedAt.Year == timeProvider.GetUtcNow().Year && x.CreatedAt.Month == timeProvider.GetUtcNow().Month).Sum(x => x.CostKrw), options.Value.MonthlyLimit);
                if (!new UsageBudgetService().CanStart(ResearchRunKind.Scheduled, usage, SystemActor()).Allowed) continue;
                var service = scope.ServiceProvider.GetRequiredService<ResearchRunService>();
                foreach (var profile in (await profiles.ListAsync(stoppingToken)).Where(x => x.IsActive)) await service.StartAsync(ResearchRunKind.Scheduled, profile, cancellationToken: stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception exception) { logger.LogError(exception, "Weekly prospect research scheduling failed."); }
        }
    }

    public static TimeSpan NextDelay(DateTimeOffset utcNow, DayOfWeek day, TimeOnly time)
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById(OperatingSystem.IsWindows() ? "Korea Standard Time" : "Asia/Seoul");
        var localNow = TimeZoneInfo.ConvertTime(utcNow, zone); var days = ((int)day - (int)localNow.DayOfWeek + 7) % 7;
        var target = localNow.Date.AddDays(days).Add(time.ToTimeSpan()); if (target <= localNow.DateTime) target = target.AddDays(7);
        return TimeZoneInfo.ConvertTimeToUtc(target, zone) - utcNow.UtcDateTime;
    }
    private static User SystemActor() => new() { LoginId = "research-scheduler", PasswordHash = "disabled", Name = "Research Scheduler", Email = "system@switchkorea.com", Role = Role.Manager };
}
