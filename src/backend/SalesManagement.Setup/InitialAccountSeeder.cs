using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SalesManagement.Core.Models;
using SalesManagement.Core.Repositories;
using SalesManagement.Postgres;

namespace SalesManagement.Setup;

public sealed class InitialAccountSeeder(
    SalesDbContext db,
    IUserRepository users,
    IPasswordHasher<User> passwordHasher)
{
    public async Task<SeedResult> SeedAsync(
        IReadOnlyList<InitialAccountDefinition> definitions,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        if (definitions.Select(x => x.LoginId).Distinct(StringComparer.OrdinalIgnoreCase).Count() != definitions.Count)
            throw new ArgumentException("Initial login IDs must be unique.", nameof(definitions));

        var created = new List<InitialCredential>();
        var existingCount = 0;
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        foreach (var definition in definitions)
        {
            if (await users.FindByLoginIdAsync(definition.LoginId, cancellationToken) is not null)
            {
                existingCount++;
                continue;
            }

            var temporaryPassword = GenerateTemporaryPassword();
            var user = new User
            {
                LoginId = definition.LoginId,
                PasswordHash = string.Empty,
                Name = definition.Name,
                Email = definition.Email,
                Role = definition.Role,
                PasswordChangeRequired = true,
            };
            user.PasswordHash = passwordHasher.HashPassword(user, temporaryPassword);
            await users.AddAsync(user, cancellationToken);
            created.Add(new InitialCredential(user.LoginId, temporaryPassword));
        }

        await transaction.CommitAsync(cancellationToken);
        return new SeedResult(created, existingCount);
    }

    private static string GenerateTemporaryPassword()
    {
        Span<byte> bytes = stackalloc byte[24];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }
}
