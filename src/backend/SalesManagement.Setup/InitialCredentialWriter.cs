using System.Security.AccessControl;
using System.Security.Principal;
using System.Runtime.Versioning;
using System.Text;

namespace SalesManagement.Setup;

public sealed class InitialCredentialWriter
{
    private const string DefaultAllowedRoot = @"C:\SwitchKorea\Secrets";
    private readonly string allowedRoot;

    public InitialCredentialWriter()
        : this(Environment.GetEnvironmentVariable("SWITCHKOREA_SECRETS_DIRECTORY") ?? DefaultAllowedRoot)
    {
    }

    public InitialCredentialWriter(string allowedRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(allowedRoot);
        this.allowedRoot = Path.GetFullPath(allowedRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    public void Write(string path, IReadOnlyList<InitialCredential> credentials)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(credentials);

        var lines = credentials.Select(x => $"{x.LoginId}\t{x.TemporaryPassword}");
        WriteProtected(path, string.Join(Environment.NewLine, lines) + Environment.NewLine);
    }

    public void WriteHash(string path, string passwordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        WriteProtected(path, passwordHash + Environment.NewLine);
    }

    private void WriteProtected(string path, string contents)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var fullPath = Path.GetFullPath(path);
        var relativePath = Path.GetRelativePath(allowedRoot, fullPath);
        if (Path.IsPathRooted(relativePath) || relativePath == ".." || relativePath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            throw new InvalidOperationException($"Credential files must be written beneath {allowedRoot}.");

        var parent = Path.GetDirectoryName(fullPath)!;
        EnsureSecureDirectory(parent);
        var created = false;
        try
        {
            if (OperatingSystem.IsWindows())
            {
                using var stream = CreateWindowsProtectedFile(fullPath);
                created = true;
                using var writer = new StreamWriter(stream, new UTF8Encoding(false));
                writer.Write(contents);
            }
            else
            {
                var options = new FileStreamOptions
                {
                    Mode = FileMode.CreateNew,
                    Access = FileAccess.Write,
                    Share = FileShare.None,
                    UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite,
                };
                using var stream = new FileStream(fullPath, options);
                created = true;
                using var writer = new StreamWriter(stream, new UTF8Encoding(false));
                writer.Write(contents);
            }
        }
        catch
        {
            if (created)
                File.Delete(fullPath);
            throw;
        }
    }

    private void EnsureSecureDirectory(string directory)
    {
        Directory.CreateDirectory(allowedRoot);
        RejectReparsePoint(allowedRoot);
        if (!OperatingSystem.IsWindows())
            File.SetUnixFileMode(allowedRoot, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);

        var relative = Path.GetRelativePath(allowedRoot, directory);
        var current = allowedRoot;
        foreach (var segment in relative.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, segment);
            if (Directory.Exists(current))
                RejectReparsePoint(current);
            else
                Directory.CreateDirectory(current);
            RejectReparsePoint(current);
            if (!OperatingSystem.IsWindows())
                File.SetUnixFileMode(current, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
    }

    private static void RejectReparsePoint(string path)
    {
        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
            throw new InvalidOperationException("Credential paths cannot contain symbolic links or reparse points.");
    }

    [SupportedOSPlatform("windows")]
    private static FileStream CreateWindowsProtectedFile(string path)
    {
        var currentUser = WindowsIdentity.GetCurrent().User
            ?? throw new InvalidOperationException("The current Windows user SID is unavailable.");
        var administrators = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);
        var security = new FileSecurity();
        security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
        security.AddAccessRule(new FileSystemAccessRule(currentUser, FileSystemRights.FullControl, AccessControlType.Allow));
        security.AddAccessRule(new FileSystemAccessRule(administrators, FileSystemRights.FullControl, AccessControlType.Allow));
        return FileSystemAclExtensions.Create(
            new FileInfo(path),
            FileMode.CreateNew,
            FileSystemRights.FullControl,
            FileShare.None,
            bufferSize: 4096,
            FileOptions.None,
            security);
    }
}
