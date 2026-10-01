using System.Text.Json;
using SalesManagement.Core.Auth;
using SalesManagement.Core.Models;
using SalesManagement.Core.Repositories;

namespace SalesManagement.Core.Targets;

public sealed class TargetService(ITargetRepository targets, IAuditRepository audits)
{
    public Task<IReadOnlyList<SalesTarget>> ListAsync(YearMonth month, CancellationToken ct) => targets.ListByMonthAsync(month, ct);

    public async Task<SalesTarget> SaveAsync(YearMonth month, Guid userId, decimal targetKrw, long expectedVersion, User actor, CancellationToken ct)
    {
        AuthorizationService.EnsureManager(actor);
        if (targetKrw < 0m) throw new ArgumentOutOfRangeException(nameof(targetKrw));
        var current = (await targets.ListByMonthAsync(month, ct)).SingleOrDefault(x => x.UserId == userId);
        var beforeTarget = current?.TargetKrw;
        SalesTarget saved;
        if (current is null)
            saved = await targets.AddAsync(new SalesTarget { YearMonth = month, UserId = userId, TargetKrw = targetKrw }, ct);
        else
        {
            current.TargetKrw = targetKrw;
            saved = await targets.UpdateAsync(current, expectedVersion, ct);
        }
        await audits.AddAsync(new AuditEntry
        {
            ActorUserId = actor.Id, Target = $"sales-target:{month}:{userId}", Action = "update",
            BeforeJson = beforeTarget is null ? null : JsonSerializer.Serialize(new { TargetKrw = beforeTarget }),
            AfterJson = JsonSerializer.Serialize(new { saved.TargetKrw })
        }, ct);
        return saved;
    }
}
