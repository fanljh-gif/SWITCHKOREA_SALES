using System.Collections.Concurrent;
using SalesManagement.Core.Exceptions;
using SalesManagement.Core.Models;
using SalesManagement.Core.Repositories;

namespace SalesManagement.InMemory.Repositories;

public abstract class InMemoryRepository<T> : IRepository<T> where T : EntityBase
{
    protected readonly ConcurrentDictionary<Guid, T> Items = new();

    public Task<IReadOnlyList<T>> ListAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<T>>(Items.Values.OrderBy(x => x.CreatedAt).ToArray());
    }

    public Task<T?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Items.TryGetValue(id, out var entity);
        return Task.FromResult(entity);
    }

    public Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!Items.TryAdd(entity.Id, entity)) throw new InvalidOperationException($"Entity {entity.Id} already exists.");
        return Task.FromResult(entity);
    }

    public Task<T> UpdateAsync(T entity, long expectedVersion, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (Items)
        {
            if (!Items.TryGetValue(entity.Id, out var current)) throw new KeyNotFoundException($"Entity {entity.Id} was not found.");
            if (current.Version != expectedVersion) throw new ConcurrencyException(entity.Id, expectedVersion, current.Version);
            entity.Version = expectedVersion + 1;
            entity.UpdatedAt = DateTimeOffset.UtcNow;
            Items[entity.Id] = entity;
            return Task.FromResult(entity);
        }
    }
}

public sealed class InMemoryUserRepository : InMemoryRepository<User>, IUserRepository
{
    public Task<User?> FindByLoginIdAsync(string loginId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Items.Values.SingleOrDefault(x => x.LoginId.Equals(loginId, StringComparison.OrdinalIgnoreCase)));
    }
}

public sealed class InMemoryCustomerRepository : InMemoryRepository<Customer>, ICustomerRepository
{
    public Task<IReadOnlyList<Customer>> ListByOwnerAsync(Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<Customer>>(Items.Values.Where(x => x.OwnerUserId == ownerUserId).ToArray());
    }
}

public sealed class InMemoryProductRepository : InMemoryRepository<Product>, IProductRepository;

public sealed class InMemoryPriceRepository : InMemoryRepository<Price>, IPriceRepository
{
    public Task<Price?> FindEffectiveAsync(Guid customerId, Guid productId, DateOnly date, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Items.Values.Where(x => x.CustomerId == customerId && x.ProductId == productId && x.ValidFrom <= date && (x.ValidTo == null || x.ValidTo >= date)).OrderByDescending(x => x.ValidFrom).FirstOrDefault());
    }
}

public sealed class InMemoryShipmentRepository : InMemoryRepository<Shipment>, IShipmentRepository
{
    public Task<IReadOnlyList<Shipment>> ListByDateRangeAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<Shipment>>(Items.Values.Where(x => x.ShipmentDate >= from && x.ShipmentDate <= to).ToArray());
    }
}

public sealed class InMemoryTargetRepository : InMemoryRepository<SalesTarget>, ITargetRepository
{
    public Task<IReadOnlyList<SalesTarget>> ListByMonthAsync(YearMonth yearMonth, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<SalesTarget>>(Items.Values.Where(x => x.YearMonth == yearMonth).ToArray());
    }
}

public sealed class InMemoryAuditRepository : InMemoryRepository<AuditEntry>, IAuditRepository
{
    public Task<IReadOnlyList<AuditEntry>> ListByTargetAsync(string target, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<AuditEntry>>(Items.Values.Where(x => x.Target.Equals(target, StringComparison.OrdinalIgnoreCase)).ToArray());
    }
}

public sealed class InMemoryImportBatchRepository : InMemoryRepository<ImportBatch>, IImportBatchRepository;

public sealed class InMemoryRateRepository : IRateRepository
{
    private readonly ConcurrentDictionary<(YearMonth, Currency), MonthlyRate> _monthly = new();
    private readonly ConcurrentDictionary<Guid, DailyRate> _daily = new();

    public Task<IReadOnlyList<MonthlyRate>> ListMonthlyAsync(YearMonth yearMonth, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<MonthlyRate>>(_monthly.Values.Where(x => x.YearMonth == yearMonth).ToArray());
    }

    public Task<MonthlyRate?> GetMonthlyAsync(YearMonth yearMonth, Currency currency, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _monthly.TryGetValue((yearMonth, currency), out var rate);
        return Task.FromResult(rate);
    }

    public Task<MonthlyRate> SaveMonthlyAsync(MonthlyRate rate, long expectedVersion, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_monthly)
        {
            var key = (rate.YearMonth, rate.Currency);
            if (_monthly.TryGetValue(key, out var current) && current.Version != expectedVersion) throw new ConcurrencyException(current.Id, expectedVersion, current.Version);
            rate.Version = current is null ? 1 : expectedVersion + 1;
            rate.UpdatedAt = DateTimeOffset.UtcNow;
            _monthly[key] = rate;
            return Task.FromResult(rate);
        }
    }

    public Task<IReadOnlyList<DailyRate>> ListDailyAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<DailyRate>>(_daily.Values.Where(x => x.Date >= from && x.Date <= to).ToArray());
    }

    public Task<DailyRate> AddDailyAsync(DailyRate rate, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_daily.TryAdd(rate.Id, rate)) throw new InvalidOperationException($"Daily rate {rate.Id} already exists.");
        return Task.FromResult(rate);
    }
}
