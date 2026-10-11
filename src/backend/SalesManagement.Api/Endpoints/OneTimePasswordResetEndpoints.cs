using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using SalesManagement.Core.Exceptions;
using SalesManagement.Core.Models;
using SalesManagement.Core.Repositories;

namespace SalesManagement.Api.Endpoints;

public sealed record OneTimePasswordResetOptions(string ExpectedTokenSha256);

public sealed class OneTimePasswordResetGate(
    IUserRepository users,
    IAuditRepository audits,
    ITransactionRunner transactions,
    IPasswordHasher<User> passwordHasher,
    OneTimePasswordResetOptions options)
{
    private static readonly Guid ConsumptionId = Guid.Parse("7d88dfcc-735c-4c0a-8c83-44d43663e7a1");

    public async Task ResetAsync(string token, string temporaryPassword, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        ArgumentException.ThrowIfNullOrWhiteSpace(temporaryPassword);
        if (temporaryPassword.Length < 24)
            throw new ArgumentException("The temporary password is too short.", nameof(temporaryPassword));

        var suppliedHash = Convert.FromHexString(HashToken(token));
        var expectedHash = Convert.FromHexString(options.ExpectedTokenSha256);
        if (!CryptographicOperations.FixedTimeEquals(suppliedHash, expectedHash))
            throw new UnauthorizedAccessException();

        await transactions.ExecuteAsync(async ct =>
        {
            if (await audits.GetAsync(ConsumptionId, ct) is not null)
                throw new InvalidOperationException("The reset token has already been consumed.");

            var user = await users.FindByLoginIdAsync("jhlee", ct)
                ?? throw new KeyNotFoundException("The reset account was not found.");
            var expectedVersion = user.Version;
            user.PasswordHash = passwordHasher.HashPassword(user, temporaryPassword);
            user.PasswordChangeRequired = true;
            user.FailedLoginAttempts = 0;
            user.LockedUntil = null;
            await users.UpdateAsync(user, expectedVersion, ct);
            await audits.AddAsync(new AuditEntry
            {
                Id = ConsumptionId,
                ActorUserId = user.Id,
                Target = "OneTimePasswordReset",
                Action = "Consume",
            }, ct);
            return true;
        }, cancellationToken);
    }

    public static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}

public static class OneTimePasswordResetEndpoints
{
    public static IEndpointRouteBuilder MapOneTimePasswordResetEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/ops/one-time-jhlee-reset", async (
            ResetRequest request,
            OneTimePasswordResetGate gate,
            CancellationToken cancellationToken) =>
        {
            try
            {
                await gate.ResetAsync(request.Token, request.TemporaryPassword, cancellationToken);
                return Results.NoContent();
            }
            catch (UnauthorizedAccessException) { return Results.NotFound(); }
            catch (KeyNotFoundException) { return Results.NotFound(); }
            catch (InvalidOperationException) { return Results.StatusCode(StatusCodes.Status410Gone); }
            catch (ConcurrencyException) { return Results.StatusCode(StatusCodes.Status410Gone); }
            catch (ArgumentException exception) { return Results.BadRequest(new { title = exception.Message, status = 400 }); }
        });
        return endpoints;
    }

    public sealed record ResetRequest(string Token, string TemporaryPassword);
}
