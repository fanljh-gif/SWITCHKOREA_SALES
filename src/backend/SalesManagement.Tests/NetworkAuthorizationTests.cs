using SalesManagement.Core.Auth;
using SalesManagement.Core.Exceptions;
using SalesManagement.Core.Models;
using SalesManagement.InMemory.Repositories;

namespace SalesManagement.Tests;

public sealed class NetworkAuthorizationTests
{
    [Fact]
    public void Member_Can_Edit_Assigned_Company()
    {
        var actor = Member("owner");
        var company = Company(actor.Id);
        var assignment = Assignment(company.Id, actor.Id);

        AuthorizationService.EnsureCanEditNetworkCompany(actor, company, assignment);
    }

    [Fact]
    public void Member_Cannot_Edit_Other_Owners_Company()
    {
        var actor = Member("member-a");
        var owner = Member("member-b");
        var company = Company(owner.Id);

        Assert.Throws<ForbiddenException>(() =>
            AuthorizationService.EnsureCanEditNetworkCompany(actor, company, Assignment(company.Id, owner.Id)));
    }

    [Fact]
    public void Manager_Can_Edit_Any_Company()
    {
        var company = Company(Guid.NewGuid());
        AuthorizationService.EnsureCanEditNetworkCompany(Manager(), company, null);
    }

    [Fact]
    public async Task Repository_Rejects_Stale_Version()
    {
        var repository = new InMemoryNetworkCompanyRepository();
        var company = await repository.AddAsync(Company(Guid.NewGuid()));
        await repository.UpdateAsync(company, company.Version);

        await Assert.ThrowsAsync<ConcurrencyException>(() => repository.UpdateAsync(company, 1));
    }

    [Fact]
    public async Task Rejected_Update_Does_Not_Mutate_Stored_Object()
    {
        var repository = new InMemoryNetworkCompanyRepository();
        var saved = await repository.AddAsync(Company(Guid.NewGuid()));
        saved.CompanyName = "변조된 이름";

        await Assert.ThrowsAsync<ConcurrencyException>(() => repository.UpdateAsync(saved, 0));

        Assert.Equal("삼성전자", (await repository.GetAsync(saved.Id))!.CompanyName);
    }

    [Fact]
    public async Task InMemory_Transaction_Restores_Repositories_On_Failure()
    {
        var repository = new InMemoryNetworkCompanyRepository();
        var runner = new InMemoryTransactionRunner(repository);

        await Assert.ThrowsAsync<InvalidOperationException>(() => runner.ExecuteAsync<int>(async ct =>
        {
            await repository.AddAsync(Company(Guid.NewGuid()), ct);
            throw new InvalidOperationException("fail");
        }));

        Assert.Empty(await repository.ListAsync());
    }

    private static NetworkCompany Company(Guid ownerId) => new()
    {
        CompanyName = "삼성전자",
        Country = "KR",
        Industry = "전자",
        LifecycleType = LifecycleType.Customer,
        IsOem = true,
        CurrentOwnerUserId = ownerId,
    };

    private static CompanyAssignment Assignment(Guid companyId, Guid userId) => new()
    {
        CompanyId = companyId,
        UserId = userId,
        ValidFrom = new DateOnly(2026, 1, 1),
        IsCurrent = true,
    };

    private static User Manager() => new()
    {
        LoginId = "manager", PasswordHash = "hash", Name = "이재학",
        Email = "jhlee@switchkorea.com", Role = Role.Manager,
    };

    private static User Member(string loginId) => new()
    {
        LoginId = loginId, PasswordHash = "hash", Name = loginId,
        Email = $"{loginId}@switchkorea.com", Role = Role.Member,
    };
}
