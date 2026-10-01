using Microsoft.AspNetCore.Identity;
using SalesManagement.Core.Audit;
using SalesManagement.Core.Auth;
using SalesManagement.Core.Models;
using SalesManagement.Core.Repositories;

namespace SalesManagement.Core.Admin;

public sealed record CreateUserCommand(string LoginId, string InitialPassword, string Name, string Email, Role Role);

public sealed class AdminUserService(IUserRepository users, IPasswordHasher<User> passwordHasher, AuditService audit)
{
    public Task<IReadOnlyList<User>> ListAsync(CancellationToken ct) => users.ListAsync(ct);

    public async Task<User> CreateAsync(CreateUserCommand command, User actor, CancellationToken ct)
    {
        AuthorizationService.EnsureManager(actor);
        if (await users.FindByLoginIdAsync(command.LoginId, ct) is not null) throw new InvalidOperationException("이미 사용 중인 로그인 ID입니다.");
        var user = new User { LoginId = command.LoginId, PasswordHash = string.Empty, Name = command.Name, Email = command.Email, Role = command.Role, PasswordChangeRequired = true };
        user.PasswordHash = passwordHasher.HashPassword(user, command.InitialPassword);
        var saved = await users.AddAsync(user, ct);
        await audit.RecordAsync(actor, $"user:{saved.Id}", "create", null, new Dictionary<string, object?> { ["Name"] = saved.Name, ["Email"] = saved.Email, ["Role"] = saved.Role, ["PasswordHash"] = saved.PasswordHash }, ct);
        return saved;
    }

    public async Task<User> SetStatusAsync(Guid id, EntityStatus status, long expectedVersion, User actor, CancellationToken ct)
    {
        AuthorizationService.EnsureManager(actor);
        var user = await users.GetAsync(id, ct) ?? throw new KeyNotFoundException();
        var before = user.Status; user.Status = status;
        var saved = await users.UpdateAsync(user, expectedVersion, ct);
        await audit.RecordAsync(actor, $"user:{id}", "status", new Dictionary<string, object?> { ["Status"] = before }, new Dictionary<string, object?> { ["Status"] = status }, ct);
        return saved;
    }

    public async Task<User> SetRoleAsync(Guid id, Role role, long expectedVersion, User actor, CancellationToken ct)
    {
        AuthorizationService.EnsureManager(actor);
        var user = await users.GetAsync(id, ct) ?? throw new KeyNotFoundException();
        var before = user.Role; user.Role = role;
        var saved = await users.UpdateAsync(user, expectedVersion, ct);
        await audit.RecordAsync(actor, $"user:{id}", "role", new Dictionary<string, object?> { ["Role"] = before }, new Dictionary<string, object?> { ["Role"] = role }, ct);
        return saved;
    }
}
