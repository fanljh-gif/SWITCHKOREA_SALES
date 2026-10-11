using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Buffers.Binary;
using SalesManagement.Core.Models;
using SalesManagement.Postgres;
using SalesManagement.Postgres.Repositories;
using SalesManagement.Setup;

namespace SalesManagement.Tests;

public sealed class PasswordResetTests
{
    [Fact]
    public void Generated_artifact_contains_a_secure_temporary_password_and_valid_identity_hash()
    {
        var generator = new PasswordResetArtifactGenerator(new PasswordHasher<User>());

        var artifact = generator.Generate("jhlee");

        Assert.True(artifact.TemporaryPassword.Length >= 24);
        var user = User("jhlee", artifact.PasswordHash);
        Assert.Equal(
            PasswordVerificationResult.Success,
            new PasswordHasher<User>().VerifyHashedPassword(user, artifact.PasswordHash, artifact.TemporaryPassword));
    }

    [Fact]
    public async Task Reset_replaces_only_the_requested_users_hash_and_clears_login_lock()
    {
        await using var db = NewDatabase();
        var original = User("jhlee", "old-hash");
        original.PasswordChangeRequired = false;
        original.FailedLoginAttempts = 5;
        original.LockedUntil = DateTimeOffset.UtcNow.AddHours(1);
        var other = User("twpark", "other-hash");
        db.Users.AddRange(original, other);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var artifact = new PasswordResetArtifactGenerator(new PasswordHasher<User>()).Generate("jhlee");

        await new PasswordResetService(new UserRepository(db)).ResetAsync("jhlee", artifact.PasswordHash);

        var saved = await db.Users.AsNoTracking().SingleAsync(x => x.LoginId == "jhlee");
        Assert.Equal(artifact.PasswordHash, saved.PasswordHash);
        Assert.True(saved.PasswordChangeRequired);
        Assert.Equal(0, saved.FailedLoginAttempts);
        Assert.Null(saved.LockedUntil);
        Assert.Equal("other-hash", (await db.Users.AsNoTracking().SingleAsync(x => x.LoginId == "twpark")).PasswordHash);
    }

    [Fact]
    public async Task Reset_rejects_an_invalid_hash_and_a_missing_login()
    {
        await using var db = NewDatabase();
        db.Users.Add(User("jhlee", "old-hash"));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var service = new PasswordResetService(new UserRepository(db));

        await Assert.ThrowsAsync<ArgumentException>(() => service.ResetAsync("jhlee", "not-a-password-hash"));
        var validHash = new PasswordResetArtifactGenerator(new PasswordHasher<User>()).Generate("missing").PasswordHash;
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.ResetAsync("missing", validHash));
        Assert.Equal("old-hash", (await db.Users.AsNoTracking().SingleAsync()).PasswordHash);
    }

    [Fact]
    public async Task Reset_rejects_malformed_identity_hash_headers_without_changing_the_password()
    {
        await using var db = NewDatabase();
        db.Users.Add(User("jhlee", "old-hash"));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var service = new PasswordResetService(new UserRepository(db));
        var malformed = new[]
        {
            IdentityHash(marker: 0, prf: 2, iterations: 100_000, saltLength: 16, subkeyLength: 32),
            IdentityHash(marker: 1, prf: 99, iterations: 100_000, saltLength: 16, subkeyLength: 32),
            IdentityHash(marker: 1, prf: 2, iterations: 0, saltLength: 16, subkeyLength: 32),
            IdentityHash(marker: 1, prf: 2, iterations: 100_000, saltLength: 0, subkeyLength: 32),
            IdentityHash(marker: 1, prf: 2, iterations: 100_000, saltLength: 16, subkeyLength: 8),
        };

        foreach (var passwordHash in malformed)
            await Assert.ThrowsAsync<ArgumentException>(() => service.ResetAsync("jhlee", passwordHash));

        Assert.Equal("old-hash", (await db.Users.AsNoTracking().SingleAsync()).PasswordHash);
    }

    [Fact]
    public void Credential_writer_creates_owner_only_files_and_rejects_symbolic_link_paths()
    {
        if (OperatingSystem.IsWindows()) return;
        var root = Path.Combine(Path.GetTempPath(), $"switch-korea-reset-{Guid.NewGuid():N}");
        var outside = Path.Combine(Path.GetTempPath(), $"switch-korea-outside-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        Directory.CreateDirectory(outside);
        try
        {
            var writer = new InitialCredentialWriter(root);
            var credentialPath = Path.Combine(root, "credential.txt");
            writer.Write(credentialPath, [new InitialCredential("jhlee", "temporary-secret")]);

            Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, File.GetUnixFileMode(credentialPath));
            var redirected = Path.Combine(root, "redirect");
            Directory.CreateSymbolicLink(redirected, outside);
            Assert.Throws<InvalidOperationException>(() => writer.WriteHash(Path.Combine(redirected, "hash.txt"), "safe-hash"));
            Assert.False(File.Exists(Path.Combine(outside, "hash.txt")));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
            Directory.Delete(outside, recursive: true);
        }
    }

    private static string IdentityHash(byte marker, uint prf, uint iterations, uint saltLength, int subkeyLength)
    {
        var bytes = new byte[13 + saltLength + subkeyLength];
        bytes[0] = marker;
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(1, 4), prf);
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(5, 4), iterations);
        BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(9, 4), saltLength);
        return Convert.ToBase64String(bytes);
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
