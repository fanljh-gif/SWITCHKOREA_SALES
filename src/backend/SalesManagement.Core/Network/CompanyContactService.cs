using SalesManagement.Core.Auth;
using SalesManagement.Core.Exceptions;
using SalesManagement.Core.Models;
using SalesManagement.Core.Repositories;

namespace SalesManagement.Core.Network;

public sealed record CompanyContactSaveCommand(Guid? Id, string Name, string Department, string Position, string BusinessRole,
    string MobilePhone, string Email, DateOnly ValidFrom, DateOnly? ValidTo, EmploymentStatus EmploymentStatus);

public sealed class CompanyContactService(ICompanyContactRepository contacts, INetworkCompanyRepository companies, ICompanyAssignmentRepository assignments,
    IAuditRepository? audits = null)
{
    public Task<IReadOnlyList<CompanyContact>> ListAsync(Guid companyId, CancellationToken ct = default) => contacts.ListByCompanyAsync(companyId, ct);
    public async Task<CompanyContact> SaveContactAsync(Guid companyId, CompanyContactSaveCommand command, User actor, long expectedVersion = 0, CancellationToken ct = default)
    {
        if (command.ValidTo < command.ValidFrom) throw new NetworkValidationException("종료일은 시작일보다 빠를 수 없습니다.");
        if (command.EmploymentStatus == EmploymentStatus.Former && command.ValidTo is null) throw new NetworkValidationException("퇴사 담당자는 종료일이 필요합니다.");
        if (string.IsNullOrWhiteSpace(command.Name) || string.IsNullOrWhiteSpace(command.Email)) throw new NetworkValidationException("성명과 이메일은 필수입니다.");
        var company = await companies.GetAsync(companyId, ct) ?? throw new KeyNotFoundException("거래선을 찾을 수 없습니다.");
        AuthorizationService.EnsureCanEditNetworkCompany(actor, company, (await assignments.ListByCompanyAsync(companyId, ct)).SingleOrDefault(x => x.IsCurrent));
        if (command.Id is null)
        {
            var created = await contacts.AddAsync(Create(companyId, command), ct);
            await RecordAudit(actor, created, "Created", ct);
            return created;
        }
        var contact = await contacts.GetAsync(command.Id.Value, ct) ?? throw new KeyNotFoundException("담당자를 찾을 수 없습니다.");
        if (contact.CompanyId != companyId) throw new NetworkValidationException("담당자와 회사가 일치하지 않습니다.");
        if (contact.Version != expectedVersion) throw new ConcurrencyException(contact.Id, expectedVersion, contact.Version);
        contact.Name = command.Name; contact.Department = command.Department; contact.Position = command.Position; contact.BusinessRole = command.BusinessRole;
        contact.MobilePhone = command.MobilePhone; contact.Email = command.Email; contact.ValidFrom = command.ValidFrom; contact.ValidTo = command.ValidTo; contact.EmploymentStatus = command.EmploymentStatus;
        var updated = await contacts.UpdateAsync(contact, expectedVersion, ct);
        await RecordAudit(actor, updated, "Updated", ct);
        return updated;
    }

    private static CompanyContact Create(Guid companyId, CompanyContactSaveCommand x) => new()
    { CompanyId = companyId, Name = x.Name, Department = x.Department, Position = x.Position, BusinessRole = x.BusinessRole,
      MobilePhone = x.MobilePhone, Email = x.Email, ValidFrom = x.ValidFrom, ValidTo = x.ValidTo, EmploymentStatus = x.EmploymentStatus };

    private Task RecordAudit(User actor, CompanyContact contact, string action, CancellationToken ct) => audits is null
        ? Task.CompletedTask
        : audits.AddAsync(new AuditEntry { ActorUserId = actor.Id, Target = contact.CompanyId.ToString(), Action = action,
            AfterJson = $"{{\"CompanyId\":\"{contact.CompanyId}\",\"EmploymentStatus\":\"{contact.EmploymentStatus}\"}}" }, ct);
}
