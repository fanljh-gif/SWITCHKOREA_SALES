using SalesManagement.Core.Models;
namespace SalesManagement.Core.Repositories;
public interface IAuditRepository : IRepository<AuditEntry> { Task<IReadOnlyList<AuditEntry>> ListByTargetAsync(string target, CancellationToken cancellationToken = default); }
