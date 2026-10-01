using Microsoft.AspNetCore.Identity;
using SalesManagement.Core.Exceptions;
using SalesManagement.Core.Models;
using SalesManagement.Core.Repositories;

namespace SalesManagement.Core.Auth;

public sealed record AuthOptions(int MaxFailedAttempts, TimeSpan LockoutDuration, TimeSpan SessionIdleTimeout)
{
    public static AuthOptions Default { get; } = new(5, TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(30));
}

public sealed record LoginResult(User User, bool PasswordChangeRequired);

public sealed class AuthService(IUserRepository users, IPasswordHasher<User> passwordHasher, TimeProvider timeProvider, AuthOptions options)
{
    public async Task<LoginResult> LoginAsync(string loginId, string password, CancellationToken cancellationToken = default)
    {
        var user = await users.FindByLoginIdAsync(loginId, cancellationToken) ?? throw new AuthenticationFailedException();
        AuthorizationService.EnsureActive(user);
        var now = timeProvider.GetUtcNow();
        if (user.LockedUntil > now) throw new AccountLockedException(user.LockedUntil.Value);
        var expectedVersion = user.Version;
        var verification = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (verification == PasswordVerificationResult.Failed)
        {
            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= options.MaxFailedAttempts) user.LockedUntil = now.Add(options.LockoutDuration);
            await users.UpdateAsync(user, expectedVersion, cancellationToken);
            throw new AuthenticationFailedException();
        }
        user.FailedLoginAttempts = 0;
        user.LockedUntil = null;
        user.LastActivityAt = now;
        await users.UpdateAsync(user, expectedVersion, cancellationToken);
        return new LoginResult(user, user.PasswordChangeRequired);
    }

    public async Task ChangePasswordAsync(User user, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        var verification = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, currentPassword);
        if (verification == PasswordVerificationResult.Failed) throw new AuthenticationFailedException();
        var expectedVersion = user.Version;
        user.PasswordHash = passwordHasher.HashPassword(user, newPassword);
        user.PasswordChangeRequired = false;
        await users.UpdateAsync(user, expectedVersion, cancellationToken);
    }

    public bool IsSessionActive(User user)
        => user.LastActivityAt is { } lastActivity && timeProvider.GetUtcNow() - lastActivity <= options.SessionIdleTimeout;
}
