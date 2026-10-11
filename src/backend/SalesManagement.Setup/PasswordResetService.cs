using System.Buffers.Binary;
using SalesManagement.Core.Repositories;

namespace SalesManagement.Setup;

public sealed class PasswordResetService(IUserRepository users)
{
    public async Task ResetAsync(string loginId, string passwordHash, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(loginId);
        ValidatePasswordHash(passwordHash);
        var user = await users.FindByLoginIdAsync(loginId, cancellationToken)
            ?? throw new KeyNotFoundException($"User '{loginId}' was not found.");
        var expectedVersion = user.Version;
        user.PasswordHash = passwordHash;
        user.PasswordChangeRequired = true;
        user.FailedLoginAttempts = 0;
        user.LockedUntil = null;
        await users.UpdateAsync(user, expectedVersion, cancellationToken);
    }

    private static void ValidatePasswordHash(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        try
        {
            var decoded = Convert.FromBase64String(value);
            if (decoded.Length < 13 || decoded[0] != 0x01)
                throw new ArgumentException("The password hash is not a supported ASP.NET Identity hash.", nameof(value));

            var prf = BinaryPrimitives.ReadUInt32BigEndian(decoded.AsSpan(1, 4));
            var iterations = BinaryPrimitives.ReadUInt32BigEndian(decoded.AsSpan(5, 4));
            var saltLength = BinaryPrimitives.ReadUInt32BigEndian(decoded.AsSpan(9, 4));
            var subkeyLength = (long)decoded.Length - 13 - saltLength;
            if (prf > 2 || iterations is < 10_000 or > 10_000_000 || saltLength is < 16 or > 64 || subkeyLength is < 16 or > 64)
                throw new ArgumentException("The password hash has an unsupported ASP.NET Identity header.", nameof(value));
        }
        catch (FormatException exception)
        {
            throw new ArgumentException("The password hash is not valid Base64.", nameof(value), exception);
        }
    }
}
