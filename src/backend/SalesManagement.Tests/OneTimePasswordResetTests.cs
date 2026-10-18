using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SalesManagement.Api.Endpoints;
using SalesManagement.Core.Models;
using SalesManagement.Postgres;
using SalesManagement.Postgres.Repositories;

namespace SalesManagement.Tests;

public sealed class OneTimePasswordResetTests
{
    [Fact]
    public async Task Valid_token_resets_only_jhlee_and_can_be_consumed_once()
    {
        await using var db = NewDatabase();
        var jhlee = User("jhlee", "old-hash");
        jhlee.FailedLoginAttempts = 5;
        jhlee.LockedUntil = DateTimeOffset.UtcNow.AddHours(1);
        var other = User("twpark", "other-hash");
        db.Users.AddRange(jhlee, other);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var gate = new OneTimePasswordResetGate(
            new UserRepository(db),
            new AuditRepository(db),
            new PostgresTransactionRunner(db),
            new PasswordHasher<User>(),
            new OneTimePasswordResetOptions(OneTimePasswordResetGate.HashToken("valid-token")));

        await gate.ResetAsync("valid-token", "New-Temporary-Password-2026!");

        var saved = await db.Users.AsNoTracking().SingleAsync(x => x.LoginId == "jhlee");
        Assert.Equal(PasswordVerificationResult.Success, new PasswordHasher<User>().VerifyHashedPassword(saved, saved.PasswordHash, "New-Temporary-Password-2026!"));
        Assert.True(saved.PasswordChangeRequired);
        Assert.Equal(0, saved.FailedLoginAttempts);
        Assert.Null(saved.LockedUntil);
        Assert.Equal("other-hash", (await db.Users.AsNoTracking().SingleAsync(x => x.LoginId == "twpark")).PasswordHash);

        var freshProcessGate = new OneTimePasswordResetGate(
            new UserRepository(db),
            new AuditRepository(db),
            new PostgresTransactionRunner(db),
            new PasswordHasher<User>(),
            new OneTimePasswordResetOptions(OneTimePasswordResetGate.HashToken("valid-token")));
        await Assert.ThrowsAsync<InvalidOperationException>(() => freshProcessGate.ResetAsync("valid-token", "Another-Temporary-Password-2026!"));
    }

    [Fact]
    public async Task Invalid_token_does_not_change_or_consume_the_reset()
    {
        await using var db = NewDatabase();
        db.Users.Add(User("jhlee", "old-hash"));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var gate = new OneTimePasswordResetGate(
            new UserRepository(db),
            new AuditRepository(db),
            new PostgresTransactionRunner(db),
            new PasswordHasher<User>(),
            new OneTimePasswordResetOptions(OneTimePasswordResetGate.HashToken("valid-token")));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => gate.ResetAsync("wrong-token", "New-Temporary-Password-2026!"));
        Assert.Equal("old-hash", (await db.Users.AsNoTracking().SingleAsync()).PasswordHash);
        await gate.ResetAsync("valid-token", "New-Temporary-Password-2026!");
    }

    private static User User(string loginId, string passwordHash) => new()
    {
        LoginId = loginId,
        PasswordHash = passwordHash,
        Name = loginId,
        Email = $"{loginId}@switchkorea.com",
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
