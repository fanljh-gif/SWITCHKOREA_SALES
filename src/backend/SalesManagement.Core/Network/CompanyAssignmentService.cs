using SalesManagement.Core.Auth;
using SalesManagement.Core.Exceptions;
using SalesManagement.Core.Models;
using SalesManagement.Core.Repositories;

namespace SalesManagement.Core.Network;

public sealed class CompanyAssignmentService(ICompanyAssignmentRepository assignments, INetworkCompanyRepository companies,
    IUserRepository users, ICustomerRepository customers, IAuditRepository audits, ITransactionRunner transactions)
{
    public Task<IReadOnlyList<CompanyAssignment>> ListAsync(Guid companyId, CancellationToken ct = default) => assignments.ListByCompanyAsync(companyId, ct);

    public Task<CompanyAssignment> ChangeAssignmentAsync(Guid companyId, Guid userId, DateOnly effectiveDate,
        long expectedVersion, User actor, CancellationToken ct = default)
    {
        AuthorizationService.EnsureManager(actor);
        return transactions.ExecuteAsync(async token =>
        {
            var company = await companies.GetAsync(companyId, token) ?? throw new KeyNotFoundException("거래선을 찾을 수 없습니다.");
            if (company.Version != expectedVersion) throw new ConcurrencyException(company.Id, expectedVersion, company.Version);
            var user = await users.GetAsync(userId, token) ?? throw new KeyNotFoundException("담당자를 찾을 수 없습니다.");
            if (user.Status != EntityStatus.Active) throw new NetworkValidationException("활성 사용자만 담당자로 지정할 수 있습니다.");
            var current = (await assignments.ListByCompanyAsync(companyId, token)).SingleOrDefault(x => x.IsCurrent);
            if (current is not null)
            {
                if (effectiveDate <= current.ValidFrom) throw new NetworkValidationException("새 담당 시작일은 기존 담당 시작일보다 늦어야 합니다.");
                var currentVersion = current.Version;
                current.ValidTo = effectiveDate.AddDays(-1); current.IsCurrent = false;
                await assignments.UpdateAsync(current, currentVersion, token);
            }
            company.CurrentOwnerUserId = userId;
            await companies.UpdateAsync(company, expectedVersion, token);
            if (company.CustomerId.HasValue && await customers.GetAsync(company.CustomerId.Value, token) is { } customer)
            {
                var customerVersion = customer.Version;
                customer.OwnerUserId = userId;
                await customers.UpdateAsync(customer, customerVersion, token);
            }
            var created = await assignments.AddAsync(new CompanyAssignment { CompanyId = companyId, UserId = userId, ValidFrom = effectiveDate, IsCurrent = true }, token);
            await audits.AddAsync(new AuditEntry { ActorUserId = actor.Id, Target = companyId.ToString(), Action = "OwnerChanged",
                BeforeJson = current is null ? null : $"{{\"UserId\":\"{current.UserId}\"}}", AfterJson = $"{{\"UserId\":\"{userId}\",\"EffectiveDate\":\"{effectiveDate:yyyy-MM-dd}\"}}" }, token);
            return created;
        }, ct);
    }
}
