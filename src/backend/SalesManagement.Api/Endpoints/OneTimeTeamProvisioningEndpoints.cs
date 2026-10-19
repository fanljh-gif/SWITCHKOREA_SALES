using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using SalesManagement.Core.Exceptions;
using SalesManagement.Core.Models;
using SalesManagement.Core.Repositories;

namespace SalesManagement.Api.Endpoints;

public sealed record OneTimeTeamProvisioningOptions(string ExpectedTokenSha256, Guid ConsumptionId);

public sealed class OneTimeTeamProvisioningGate(
    IUserRepository users,
    IAuditRepository audits,
    ITransactionRunner transactions,
    IPasswordHasher<User> passwordHasher,
    OneTimeTeamProvisioningOptions options)
{
    private static readonly AccountDefinition[] Accounts =
    [
        new("ptw0517", "twpark", "박태원", "ptw0517@switchkorea.com"),
        new("cmc", "mcchoi", "최명철", "cmc@switchkorea.com"),
        new("sylee", null, "이소연", "sylee@switchkorea.com"),
        new("cslee", null, "이창선", "cslee@switchkorea.com"),
    ];

    public async Task ProvisionAsync(
        string token,
        IReadOnlyDictionary<string, string> temporaryPasswords,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        ArgumentNullException.ThrowIfNull(temporaryPasswords);
        if (temporaryPasswords.Count != Accounts.Length ||
            Accounts.Any(account => !temporaryPasswords.TryGetValue(account.LoginId, out var value) || value.Length < 24))
            throw new ArgumentException("Exactly four temporary passwords of at least 24 characters are required.", nameof(temporaryPasswords));

        var suppliedHash = Convert.FromHexString(HashToken(token));
        var expectedHash = Convert.FromHexString(options.ExpectedTokenSha256);
        if (!CryptographicOperations.FixedTimeEquals(suppliedHash, expectedHash))
            throw new UnauthorizedAccessException();

        await transactions.ExecuteAsync(async ct =>
        {
            if (await audits.GetAsync(options.ConsumptionId, ct) is not null)
                throw new InvalidOperationException("The provisioning token has already been consumed.");

            var provisioned = new List<User>(Accounts.Length);
            foreach (var account in Accounts)
            {
                var desired = await users.FindByLoginIdAsync(account.LoginId, ct);
                var legacy = account.LegacyLoginId is null
                    ? null
                    : await users.FindByLoginIdAsync(account.LegacyLoginId, ct);
                if (desired is not null && legacy is not null && desired.Id != legacy.Id)
                    throw new InvalidOperationException($"Both desired and legacy accounts exist for {account.LoginId}.");

                var user = desired ?? legacy;
                if (user is null)
                {
                    user = NewUser(account);
                    user.PasswordHash = passwordHasher.HashPassword(user, temporaryPasswords[account.LoginId]);
                    await users.AddAsync(user, ct);
                }
                else
                {
                    var expectedVersion = user.Version;
                    Apply(account, user);
                    user.PasswordHash = passwordHasher.HashPassword(user, temporaryPasswords[account.LoginId]);
                    await users.UpdateAsync(user, expectedVersion, ct);
                }
                provisioned.Add(user);
            }

            await audits.AddAsync(new AuditEntry
            {
                Id = options.ConsumptionId,
                ActorUserId = provisioned[0].Id,
                Target = "OneTimeTeamProvisioning",
                Action = "Consume",
            }, ct);
            return true;
        }, cancellationToken);
    }

    private static User NewUser(AccountDefinition account)
    {
        var user = new User
        {
            LoginId = account.LoginId,
            PasswordHash = string.Empty,
            Name = account.Name,
            Email = account.Email,
        };
        Apply(account, user);
        return user;
    }

    private static void Apply(AccountDefinition account, User user)
    {
        user.LoginId = account.LoginId;
        user.Name = account.Name;
        user.Email = account.Email;
        user.Role = Role.Member;
        user.Status = EntityStatus.Active;
        user.PasswordChangeRequired = true;
        user.FailedLoginAttempts = 0;
        user.LockedUntil = null;
    }

    public static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private sealed record AccountDefinition(string LoginId, string? LegacyLoginId, string Name, string Email);
}

public static class OneTimeTeamProvisioningEndpoints
{
    public static IEndpointRouteBuilder MapOneTimeTeamProvisioningEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/ops/one-time-team-provision", async (
            ProvisionRequest request,
            OneTimeTeamProvisioningGate gate,
            CancellationToken cancellationToken) =>
        {
            try
            {
                await gate.ProvisionAsync(request.Token, request.TemporaryPasswords, cancellationToken);
                return Results.NoContent();
            }
            catch (UnauthorizedAccessException) { return Results.NotFound(); }
            catch (InvalidOperationException) { return Results.StatusCode(StatusCodes.Status410Gone); }
            catch (ConcurrencyException) { return Results.StatusCode(StatusCodes.Status410Gone); }
            catch (ArgumentException exception) { return Results.BadRequest(new { title = exception.Message, status = 400 }); }
        });
        return endpoints;
    }

    public sealed record ProvisionRequest(string Token, Dictionary<string, string> TemporaryPasswords);
}
