using SalesManagement.Core.Models;
namespace SalesManagement.Core.Repositories;
public interface ITargetRepository : IRepository<SalesTarget> { Task<IReadOnlyList<SalesTarget>> ListByMonthAsync(YearMonth yearMonth, CancellationToken cancellationToken = default); }
