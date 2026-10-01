using SalesManagement.Core.Repositories;
namespace SalesManagement.Postgres;
public sealed class PostgresTransactionRunner(SalesDbContext db) : ITransactionRunner
{
    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        try { var result = await action(ct); await transaction.CommitAsync(ct); return result; }
        catch { await transaction.RollbackAsync(ct); throw; }
    }
}
