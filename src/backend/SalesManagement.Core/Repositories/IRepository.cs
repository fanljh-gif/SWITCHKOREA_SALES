using SalesManagement.Core.Models;

namespace SalesManagement.Core.Repositories;

public interface IRepository<T> where T : EntityBase
{
    Task<IReadOnlyList<T>> ListAsync(CancellationToken cancellationToken = default);
    Task<T?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);
    Task<T> UpdateAsync(T entity, long expectedVersion, CancellationToken cancellationToken = default);
}
