using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SalesManagement.Core.Models;
using SalesManagement.Postgres;
using SalesManagement.Postgres.Repositories;
using SalesManagement.Setup;

namespace SalesManagement.Tests;

public sealed class InitialAccountSeederTests
{
    private static readonly IReadOnlyList<InitialAccountDefinition> ExpectedAccounts =
    [
        new("jhlee", "이재학", "jhlee@switchkorea.com", Role.Manager),
        new("twpark", "박태원", "ptw0517@switchkorea.com", Role.Member),
        new("mcchoi", "최명철", "cmc@switchkorea.com", Role.Member),
        new("sylee", "이소연", "sylee@switchkorea.com", Role.Member),
        new("cslee", "이창선", "cslee@switchkorea.com", Role.Member),
    ];

    [Fact]
    public async Task Seed_creates_exact_approved_accounts_with_distinct_secure_passwords()
    {
        await using var db = NewDatabase();
        var seeder = new InitialAccountSeeder(db, new UserRepository(db), new PasswordHasher<User>());

        var result = await seeder.SeedAsync(ExpectedAccounts, CancellationToken.None);

        Assert.Equal(5, result.Created.Count);
        Assert.Equal(0, result.ExistingCount);
        Assert.Equal(5, result.Created.Select(x => x.TemporaryPassword).Distinct().Count());
        Assert.All(result.Created, x => Assert.True(x.TemporaryPassword.Length >= 16));

        var users = await db.Users.OrderBy(x => x.LoginId).ToListAsync(CancellationToken.None);
        Assert.Equal(ExpectedAccounts.OrderBy(x => x.LoginId).Select(x => (x.LoginId, x.Name, x.Email, x.Role)),
            users.Select(x => (x.LoginId, x.Name, x.Email, x.Role)));
        Assert.All(users, x => Assert.True(x.PasswordChangeRequired));
        Assert.All(users, x => Assert.NotEmpty(x.PasswordHash));
    }

    [Fact]
    public async Task Seed_is_idempotent_and_preserves_existing_password_hashes()
    {
        await using var db = NewDatabase();
        var seeder = new InitialAccountSeeder(db, new UserRepository(db), new PasswordHasher<User>());
        await seeder.SeedAsync(ExpectedAccounts, CancellationToken.None);
        var hashes = await db.Users.ToDictionaryAsync(x => x.LoginId, x => x.PasswordHash, CancellationToken.None);

        var second = await seeder.SeedAsync(ExpectedAccounts, CancellationToken.None);

        Assert.Empty(second.Created);
        Assert.Equal(5, second.ExistingCount);
        var current = await db.Users.ToDictionaryAsync(x => x.LoginId, x => x.PasswordHash, CancellationToken.None);
        Assert.Equal(hashes, current);
    }

    private static SalesDbContext NewDatabase()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<SalesDbContext>().UseSqlite(connection).Options;
        var db = new SalesDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }
}
