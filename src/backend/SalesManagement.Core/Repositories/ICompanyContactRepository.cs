using SalesManagement.Core.Models;
namespace SalesManagement.Core.Repositories;
public interface ICompanyContactRepository : IRepository<CompanyContact>
{
    Task<IReadOnlyList<CompanyContact>> ListByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);
}
