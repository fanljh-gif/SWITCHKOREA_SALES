using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SalesManagement.Api.Endpoints;
using SalesManagement.Core.Models;
using SalesManagement.Postgres;
using SalesManagement.Postgres.Repositories;
using SalesManagement.Setup;

namespace SalesManagement.Tests;

public sealed class OneTimeTeamProvisioningTests
{
    private static readonly IReadOnlyDictionary<string, string> Passwords = new Dictionary<string, string>
    {
        ["ptw0517"] = "Park-Temporary-Password-2026!",
        ["cmc"] = "Choi-Temporary-Password-2026!",
        ["sylee"] = "So-Temporary-Password-2026!",
        ["cslee"] = "Chang-Temporary-Password-2026!",
    };

    [Fact]
    public void Default_setup_accounts_use_approved_login_ids()
    {
        Assert.Equal(
            ["jhlee", "ptw0517", "cmc", "sylee", "cslee"],
            InitialAccountCatalog.All.Select(x => x.LoginId));
    }

    [Fact]
    public async Task Provision_preserves_legacy_ids_and_creates_missing_accounts()
    {
        await using var db = NewDatabase();
        var park = User("twpark", "박태원", "old-park@switchkorea.com");
        var choi = User("mcchoi", "최명철", "old-choi@switchkorea.com");
        var so = User("sylee", "이소연", "old-so@switchkorea.com");
        park.FailedLoginAttempts = 4;
        park.LockedUntil = DateTimeOffset.UtcNow.AddHours(1);
        db.Users.AddRange(park, choi, so);
        await db.SaveChangesAsync();
        var parkId = park.Id;
        var choiId = choi.Id;
        var soId = so.Id;
        db.ChangeTracker.Clear();

        var gate = Gate(db, "valid-token", Guid.Parse("9f0cc7b3-e5e0-4e24-a1fc-acde14f0d6bd"));
        await gate.ProvisionAsync("valid-token", Passwords);

        var users = await db.Users.AsNoTracking().OrderBy(x => x.LoginId).ToListAsync();
        Assert.Equal(4, users.Count);
        Assert.Equal(parkId, users.Single(x => x.LoginId == "ptw0517").Id);
        Assert.Equal(choiId, users.Single(x => x.LoginId == "cmc").Id);
        Assert.Equal(soId, users.Single(x => x.LoginId == "sylee").Id);
        Assert.DoesNotContain(users, x => x.LoginId is "twpark" or "mcchoi");

        var expected = new Dictionary<string, (string Name, string Email)>
        {
            ["ptw0517"] = ("박태원", "ptw0517@switchkorea.com"),
            ["cmc"] = ("최명철", "cmc@switchkorea.com"),
            ["sylee"] = ("이소연", "sylee@switchkorea.com"),
            ["cslee"] = ("이창선", "cslee@switchkorea.com"),
        };
        var hasher = new PasswordHasher<User>();
        foreach (var user in users)
        {
            Assert.Equal(expected[user.LoginId].Name, user.Name);
            Assert.Equal(expected[user.LoginId].Email, user.Email);
            Assert.Equal(Role.Member, user.Role);
            Assert.Equal(EntityStatus.Active, user.Status);
            Assert.True(user.PasswordChangeRequired);
            Assert.Equal(0, user.FailedLoginAttempts);
            Assert.Null(user.LockedUntil);
            Assert.Equal(PasswordVerificationResult.Success, hasher.VerifyHashedPassword(user, user.PasswordHash, Passwords[user.LoginId]));
        }

        await Assert.ThrowsAsync<InvalidOperationException>(() => gate.ProvisionAsync("valid-token", Passwords));
    }

    [Fact]
    public async Task Invalid_token_does_not_change_accounts_or_consume_operation()
    {
        await using var db = NewDatabase();
        db.Users.Add(User("twpark", "박태원", "old@switchkorea.com"));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var gate = Gate(db, "valid-token", Guid.Parse("e6176800-1d22-44cc-b6fc-a7c90b0ff27b"));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => gate.ProvisionAsync("wrong-token", Passwords));
        Assert.NotNull(await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.LoginId == "twpark"));

        await gate.ProvisionAsync("valid-token", Passwords);
        Assert.NotNull(await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.LoginId == "ptw0517"));
    }

    private static OneTimeTeamProvisioningGate Gate(SalesDbContext db, string token, Guid consumptionId) => new(
        new UserRepository(db),
        new AuditRepository(db),
        new PostgresTransactionRunner(db),
        new PasswordHasher<User>(),
        new OneTimeTeamProvisioningOptions(OneTimeTeamProvisioningGate.HashToken(token), consumptionId));

    private static User User(string loginId, string name, string email) => new()
    {
        LoginId = loginId,
        PasswordHash = "old-hash",
        Name = name,
        Email = email,
        Role = Role.Member,
    };

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
