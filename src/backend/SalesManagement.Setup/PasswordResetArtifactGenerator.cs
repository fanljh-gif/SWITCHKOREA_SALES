using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using SalesManagement.Core.Models;

namespace SalesManagement.Setup;

public sealed record PasswordResetArtifact(string LoginId, string TemporaryPassword, string PasswordHash);

public sealed class PasswordResetArtifactGenerator(IPasswordHasher<User> passwordHasher)
{
    public PasswordResetArtifact Generate(string loginId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(loginId);
        Span<byte> bytes = stackalloc byte[24];
        RandomNumberGenerator.Fill(bytes);
        var temporaryPassword = Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        var user = new User
        {
            LoginId = loginId,
            PasswordHash = string.Empty,
            Name = loginId,
            Email = $"{loginId}@reset.invalid",
        };
        return new PasswordResetArtifact(loginId, temporaryPassword, passwordHasher.HashPassword(user, temporaryPassword));
    }
}
