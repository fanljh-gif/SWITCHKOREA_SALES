using System.Collections.Concurrent;
using System.Text.Json;
using SalesManagement.Core.Exceptions;
using SalesManagement.Core.Models;
using SalesManagement.Core.Repositories;

namespace SalesManagement.InMemory.Repositories;

public interface IInMemoryTransactionalStore
{
    object Capture();
    void Restore(object snapshot);
}

public abstract class InMemoryRepository<T> : IRepository<T>, IInMemoryTransactionalStore where T : EntityBase
{
    protected readonly ConcurrentDictionary<Guid, T> Items = new();
    private static readonly JsonSerializerOptions CloneOptions = new(JsonSerializerDefaults.Web);
    protected static T Clone(T value) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, CloneOptions), CloneOptions)!;

    public Task<IReadOnlyList<T>> ListAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<T>>(Items.Values.OrderBy(x => x.CreatedAt).Select(Clone).ToArray());
    }

    public Task<T?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Items.TryGetValue(id, out var entity);
        return Task.FromResult(entity is null ? null : Clone(entity));
    }

    public Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var stored = Clone(entity);
        if (!Items.TryAdd(stored.Id, stored)) throw new InvalidOperationException($"Entity {entity.Id} already exists.");
        return Task.FromResult(Clone(stored));
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
            Items[entity.Id] = Clone(entity);
            return Task.FromResult(Clone(entity));
        }
    }

    public object Capture() => Items.ToDictionary(x => x.Key, x => Clone(x.Value));
    public void Restore(object snapshot)
    {
        lock (Items)
        {
            Items.Clear();
            foreach (var item in (IReadOnlyDictionary<Guid, T>)snapshot) Items[item.Key] = Clone(item.Value);
        }
    }
}

public sealed class InMemoryUserRepository : InMemoryRepository<User>, IUserRepository
{
    public Task<User?> FindByLoginIdAsync(string loginId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var value = Items.Values.SingleOrDefault(x => x.LoginId.Equals(loginId, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(value is null ? null : Clone(value));
    }
}

public sealed class InMemoryCustomerRepository : InMemoryRepository<Customer>, ICustomerRepository
{
    public Task<IReadOnlyList<Customer>> ListByOwnerAsync(Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<Customer>>(Items.Values.Where(x => x.OwnerUserId == ownerUserId).Select(Clone).ToArray());
    }
}

public sealed class InMemoryProductRepository : InMemoryRepository<Product>, IProductRepository;

public sealed class InMemoryPriceRepository : InMemoryRepository<Price>, IPriceRepository
{
    public Task<Price?> FindEffectiveAsync(Guid customerId, Guid productId, DateOnly date, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var value = Items.Values.Where(x => x.CustomerId == customerId && x.ProductId == productId && x.ValidFrom <= date && (x.ValidTo == null || x.ValidTo >= date)).OrderByDescending(x => x.ValidFrom).FirstOrDefault();
        return Task.FromResult(value is null ? null : Clone(value));
    }
}

public sealed class InMemoryShipmentRepository : InMemoryRepository<Shipment>, IShipmentRepository
{
    public Task<IReadOnlyList<Shipment>> ListByDateRangeAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<Shipment>>(Items.Values.Where(x => x.ShipmentDate >= from && x.ShipmentDate <= to).Select(Clone).ToArray());
    }
}

public sealed class InMemoryTargetRepository : InMemoryRepository<SalesTarget>, ITargetRepository
{
    public Task<IReadOnlyList<SalesTarget>> ListByMonthAsync(YearMonth yearMonth, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<SalesTarget>>(Items.Values.Where(x => x.YearMonth == yearMonth).Select(Clone).ToArray());
    }
}

public sealed class InMemoryAuditRepository : InMemoryRepository<AuditEntry>, IAuditRepository
{
    public Task<IReadOnlyList<AuditEntry>> ListByTargetAsync(string target, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<AuditEntry>>(Items.Values.Where(x => x.Target.Equals(target, StringComparison.OrdinalIgnoreCase)).Select(Clone).ToArray());
    }
}

public sealed class InMemoryImportBatchRepository : InMemoryRepository<ImportBatch>, IImportBatchRepository;

public sealed class InMemoryNetworkCompanyRepository : InMemoryRepository<NetworkCompany>, INetworkCompanyRepository
{
    public Task<IReadOnlyList<NetworkCompany>> ListByOwnerAsync(Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<NetworkCompany>>(Items.Values.Where(x => x.CurrentOwnerUserId == ownerUserId).Select(Clone).ToArray());
    }
}

public sealed class InMemoryCompanyRelationshipRepository : InMemoryRepository<CompanyRelationship>, ICompanyRelationshipRepository
{
    public Task<IReadOnlyList<CompanyRelationship>> ListByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<CompanyRelationship>>(Items.Values.Where(x => x.UpstreamCompanyId == companyId || x.DownstreamCompanyId == companyId).Select(Clone).ToArray());
    }

    public async Task<IReadOnlyList<CompanyRelationship>> ListByRootAsync(Guid rootCompanyId, CancellationToken cancellationToken = default)
    {
        var result = new List<CompanyRelationship>();
        var pending = new Stack<Guid>();
        var expanded = new HashSet<Guid>();
        pending.Push(rootCompanyId);
        while (pending.TryPop(out var companyId) && expanded.Add(companyId))
        {
            cancellationToken.ThrowIfCancellationRequested();
            foreach (var relationship in Items.Values.Where(x => x.UpstreamCompanyId == companyId && x.Status == EntityStatus.Active))
            {
                result.Add(Clone(relationship));
                pending.Push(relationship.DownstreamCompanyId);
            }
        }
        return await Task.FromResult<IReadOnlyList<CompanyRelationship>>(result);
    }
}

public sealed class InMemoryCompanyContactRepository : InMemoryRepository<CompanyContact>, ICompanyContactRepository
{
    public Task<IReadOnlyList<CompanyContact>> ListByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<CompanyContact>>(Items.Values.Where(x => x.CompanyId == companyId).Select(Clone).ToArray());
    }
}

public sealed class InMemoryCompanyAssignmentRepository : InMemoryRepository<CompanyAssignment>, ICompanyAssignmentRepository
{
    public Task<IReadOnlyList<CompanyAssignment>> ListByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<CompanyAssignment>>(Items.Values.Where(x => x.CompanyId == companyId).OrderBy(x => x.ValidFrom).Select(Clone).ToArray());
    }
}

public sealed class InMemoryTransactionRunner(params IInMemoryTransactionalStore[] stores) : ITransactionRunner
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        var snapshots = stores.Select(x => x.Capture()).ToArray();
        try { return await action(cancellationToken); }
        catch
        {
            for (var i = 0; i < stores.Length; i++) stores[i].Restore(snapshots[i]);
            throw;
        }
        finally { _gate.Release(); }
    }
}

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
