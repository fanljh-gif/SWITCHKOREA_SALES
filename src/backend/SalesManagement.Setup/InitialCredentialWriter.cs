using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;

namespace SalesManagement.Setup;

public sealed class InitialCredentialWriter
{
    private const string AllowedRoot = @"C:\SwitchKorea\Secrets";

    public void Write(string path, IReadOnlyList<InitialCredential> credentials)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(credentials);

        var fullPath = Path.GetFullPath(path);
        var root = Path.GetFullPath(AllowedRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Credential files must be written beneath {AllowedRoot}.");

        var directory = Path.GetDirectoryName(fullPath)!;
        Directory.CreateDirectory(directory);

        var lines = credentials.Select(x => $"{x.LoginId}\t{x.TemporaryPassword}");
        File.WriteAllText(fullPath, string.Join(Environment.NewLine, lines) + Environment.NewLine, new UTF8Encoding(false));
        RestrictAccess(fullPath);
    }

    private static void RestrictAccess(string path)
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Credential ACL protection requires Windows.");

        var currentUser = WindowsIdentity.GetCurrent().User
            ?? throw new InvalidOperationException("The current Windows user SID is unavailable.");
        var administrators = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);
        var security = new FileSecurity();
        security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
        security.AddAccessRule(new FileSystemAccessRule(currentUser, FileSystemRights.FullControl, AccessControlType.Allow));
        security.AddAccessRule(new FileSystemAccessRule(administrators, FileSystemRights.FullControl, AccessControlType.Allow));
        new FileInfo(path).SetAccessControl(security);
    }
}
