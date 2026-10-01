using SalesManagement.Core.Audit;
using SalesManagement.Core.Admin;
using SalesManagement.Core.Models;
using SalesManagement.InMemory.Repositories;
using Microsoft.AspNetCore.Identity;

namespace SalesManagement.Tests;

public sealed class AuditTests
{
    [Fact]
    public async Task Sensitive_Fields_Are_Excluded_From_Audit_History()
    {
        var repository = new InMemoryAuditRepository();
        var service = new AuditService(repository);
        var actor = new User { LoginId = "manager", PasswordHash = "secret-hash", Name = "이재학", Email = "jhlee@switchkorea.com", Role = Role.Manager };

        await service.RecordAsync(actor, "user:1", "update", null, new Dictionary<string, object?> { ["Name"] = actor.Name, ["PasswordHash"] = actor.PasswordHash, ["Role"] = actor.Role }, CancellationToken.None);

        var entry = Assert.Single(await service.ListAsync("user:1", CancellationToken.None));
        Assert.DoesNotContain("Password", entry.AfterJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret", entry.AfterJson, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("이재학", entry.AfterJson);
    }

    [Fact]
    public async Task Manager_Creates_User_Without_Storing_Plaintext_Password()
    {
        var users = new InMemoryUserRepository();
        var audits = new InMemoryAuditRepository();
        var manager = new User { LoginId = "manager", PasswordHash = "hash", Name = "이재학", Email = "jhlee@switchkorea.com", Role = Role.Manager };
        var service = new AdminUserService(users, new PasswordHasher<User>(), new AuditService(audits));

        var created = await service.CreateAsync(new CreateUserCommand("member", "temporary-password", "박태원", "ptw@switchkorea.com", Role.Member), manager, CancellationToken.None);

        Assert.NotEqual("temporary-password", created.PasswordHash);
        Assert.True(created.PasswordChangeRequired);
        Assert.DoesNotContain("temporary-password", Assert.Single(await audits.ListAsync()).AfterJson ?? string.Empty);
    }
}
