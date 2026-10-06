using SalesManagement.Core.Models;
namespace SalesManagement.Core.Repositories;
public interface ICompanyAssignmentRepository : IRepository<CompanyAssignment>
{
    Task<IReadOnlyList<CompanyAssignment>> ListByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);
}
