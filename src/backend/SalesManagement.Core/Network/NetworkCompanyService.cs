using SalesManagement.Core.Auth;
using SalesManagement.Core.Exceptions;
using SalesManagement.Core.Models;
using SalesManagement.Core.Repositories;
using System.Text.Json;

namespace SalesManagement.Core.Network;

public sealed record NetworkCompanySaveCommand(Guid? Id, string CompanyName, string Country, string Industry,
    LifecycleType LifecycleType, bool IsOem, Guid OwnerUserId, EntityStatus Status);

public sealed class NetworkCompanyService(INetworkCompanyRepository companies, ICustomerRepository customers,
    ICompanyAssignmentRepository assignments, IUserRepository users, IAuditRepository audits, ITransactionRunner transactions)
{
    public Task<IReadOnlyList<NetworkCompany>> ListAsync(CancellationToken ct = default) => companies.ListAsync(ct);
    public Task<NetworkCompany?> GetAsync(Guid id, CancellationToken ct = default) => companies.GetAsync(id, ct);

    public async Task<NetworkCompany> SaveCompanyAsync(NetworkCompanySaveCommand command, User actor,
        long expectedVersion = 0, CancellationToken ct = default)
    {
        AuthorizationService.EnsureActive(actor);
        Validate(command.CompanyName, command.Country, command.Industry);
        if (command.Id is null)
        {
            var createsOem = actor.Role == Role.Manager && command.IsOem && command.LifecycleType == LifecycleType.Customer;
            if (command.LifecycleType != LifecycleType.Prospect && !createsOem)
                throw new NetworkValidationException("정식 거래처는 잠재고객 전환 또는 데이터 이관으로만 생성할 수 있습니다.");
            if (command.IsOem && command.LifecycleType != LifecycleType.Customer)
                throw new NetworkValidationException("원청은 정식 거래처 구분으로 등록해야 합니다.");
            if (actor.Role != Role.Manager && (command.IsOem || command.OwnerUserId != actor.Id))
                throw new ForbiddenException("팀원은 본인 담당 잠재고객만 등록할 수 있습니다.");
            await EnsureActiveOwner(command.OwnerUserId, ct);
            return await transactions.ExecuteAsync(async token =>
            {
                var company = await companies.AddAsync(new NetworkCompany
                {
                    CompanyName = command.CompanyName.Trim(), Country = command.Country.Trim(), Industry = command.Industry.Trim(),
                    LifecycleType = command.LifecycleType, IsOem = command.IsOem,
                    CurrentOwnerUserId = command.OwnerUserId, Status = command.Status,
                }, token);
                await assignments.AddAsync(new CompanyAssignment { CompanyId = company.Id, UserId = command.OwnerUserId, ValidFrom = SeoulBusinessDate.Today(), IsCurrent = true }, token);
                await audits.AddAsync(new AuditEntry { ActorUserId = actor.Id, Target = company.Id.ToString(), Action = "Created", AfterJson = JsonSerializer.Serialize(new { company.CompanyName, LifecycleType = company.LifecycleType.ToString() }) }, token);
                return company;
            }, ct);
        }

        var existing = await companies.GetAsync(command.Id.Value, ct) ?? throw new KeyNotFoundException("영업망 거래선을 찾을 수 없습니다.");
        var current = (await assignments.ListByCompanyAsync(existing.Id, ct)).SingleOrDefault(x => x.IsCurrent);
        AuthorizationService.EnsureCanEditNetworkCompany(actor, existing, current);
        EnsureVersion(existing, expectedVersion);
        if (existing.LifecycleType != command.LifecycleType || existing.IsOem != command.IsOem)
        {
            if (actor.Role != Role.Manager) throw new ForbiddenException("팀원은 회사 분류를 변경할 수 없습니다.");
            throw new NetworkValidationException("회사 분류는 전용 등록·전환 기능을 사용해 주세요.");
        }
        if (actor.Role != Role.Manager && existing.CurrentOwnerUserId != command.OwnerUserId)
            throw new ForbiddenException("팀원은 고객 구분, OEM 구분 또는 담당자를 변경할 수 없습니다.");
        existing.CompanyName = command.CompanyName.Trim(); existing.Country = command.Country.Trim(); existing.Industry = command.Industry.Trim(); existing.Status = command.Status;
        var saved = await companies.UpdateAsync(existing, expectedVersion, ct);
        await audits.AddAsync(new AuditEntry { ActorUserId = actor.Id, Target = existing.Id.ToString(), Action = "Updated",
            AfterJson = JsonSerializer.Serialize(new { saved.CompanyName, saved.Country, saved.Industry, Status = saved.Status.ToString() }) }, ct);
        return saved;
    }

    public Task<NetworkCompany> ConvertProspectAsync(Guid companyId, string customerCode, Currency defaultCurrency,
        long expectedVersion, User actor, CancellationToken ct = default)
    {
        AuthorizationService.EnsureManager(actor);
        if (string.IsNullOrWhiteSpace(customerCode)) throw new NetworkValidationException("거래처 코드는 필수입니다.");
        return transactions.ExecuteAsync(async token =>
        {
            var company = await companies.GetAsync(companyId, token) ?? throw new KeyNotFoundException("잠재고객을 찾을 수 없습니다.");
            EnsureVersion(company, expectedVersion);
            if (company.LifecycleType != LifecycleType.Prospect) throw new NetworkValidationException("잠재고객만 거래처로 전환할 수 있습니다.");
            var customer = await customers.AddAsync(new Customer { Code = customerCode.Trim(), CompanyName = company.CompanyName, Country = company.Country, DefaultCurrency = defaultCurrency, OwnerUserId = company.CurrentOwnerUserId }, token);
            company.CustomerId = customer.Id; company.LifecycleType = LifecycleType.Customer;
            var saved = await companies.UpdateAsync(company, expectedVersion, token);
            await audits.AddAsync(new AuditEntry { ActorUserId = actor.Id, Target = company.Id.ToString(), Action = "ProspectConverted", AfterJson = $"{{\"CustomerId\":\"{customer.Id}\",\"LifecycleType\":\"Customer\"}}" }, token);
            return saved;
        }, ct);
    }

    public async Task DisableAsync(Guid companyId, long expectedVersion, User actor, CancellationToken ct = default)
    {
        var company = await companies.GetAsync(companyId, ct) ?? throw new KeyNotFoundException("영업망 거래선을 찾을 수 없습니다.");
        var current = (await assignments.ListByCompanyAsync(companyId, ct)).SingleOrDefault(x => x.IsCurrent);
        AuthorizationService.EnsureCanEditNetworkCompany(actor, company, current);
        EnsureVersion(company, expectedVersion);
        company.Status = EntityStatus.Inactive;
        await companies.UpdateAsync(company, expectedVersion, ct);
        await audits.AddAsync(new AuditEntry { ActorUserId = actor.Id, Target = company.Id.ToString(), Action = "Disabled", AfterJson = "{\"Status\":\"Inactive\"}" }, ct);
    }

    private static void Validate(string name, string country, string industry)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(country) || string.IsNullOrWhiteSpace(industry))
            throw new NetworkValidationException("회사명, 국가, 산업은 필수입니다.");
    }

    private static void EnsureVersion(NetworkCompany company, long expectedVersion)
    {
        if (company.Version != expectedVersion) throw new ConcurrencyException(company.Id, expectedVersion, company.Version);
    }

    private async Task EnsureActiveOwner(Guid ownerId, CancellationToken ct)
    {
        if (await users.GetAsync(ownerId, ct) is not { Status: EntityStatus.Active })
            throw new NetworkValidationException("활성 사용자만 담당자로 지정할 수 있습니다.");
    }
}
