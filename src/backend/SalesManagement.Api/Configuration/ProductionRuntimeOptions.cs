namespace SalesManagement.Api.Configuration;

public sealed record ProductionRuntimeOptions(string? ConnectionString, string? DataProtectionPath)
{
    public const string ConnectionStringName = "SalesManagement";
    public const string DataProtectionPathVariable = "SWITCHKOREA_DATA_PROTECTION_PATH";
    public const string ConnectionFileVariable = "SWITCHKOREA_CONNECTION_FILE";

    public static ProductionRuntimeOptions From(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName);
        var protectedPath = configuration[ConnectionFileVariable];
        if (string.IsNullOrWhiteSpace(connectionString) && !string.IsNullOrWhiteSpace(protectedPath))
            connectionString = ReadProtectedConnection(protectedPath);
        return new ProductionRuntimeOptions(connectionString, configuration[DataProtectionPathVariable]);
    }

    private static string ReadProtectedConnection(string path)
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Protected database connection files require Windows DPAPI.");
        if (!File.Exists(path))
            throw new InvalidOperationException($"Protected database connection file was not found: {path}");

        var protectedBytes = File.ReadAllBytes(path);
        var plainBytes = System.Security.Cryptography.ProtectedData.Unprotect(
            protectedBytes,
            optionalEntropy: null,
            System.Security.Cryptography.DataProtectionScope.LocalMachine);
        try { return System.Text.Encoding.UTF8.GetString(plainBytes); }
        finally { Array.Clear(plainBytes); }
    }
}
