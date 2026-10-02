using SalesManagement.Postgres.Configuration;

namespace SalesManagement.Api.Configuration;

public enum ProductionHostingMode { Windows, Render }

public sealed record ProductionRuntimeOptions(
    string? ConnectionString,
    string? DataProtectionPath,
    ProductionHostingMode HostingMode,
    string? ExternalHostname)
{
    public const string ConnectionStringName = "SalesManagement";
    public const string DataProtectionPathVariable = "SWITCHKOREA_DATA_PROTECTION_PATH";
    public const string ConnectionFileVariable = "SWITCHKOREA_CONNECTION_FILE";
    public const string DatabaseUrlVariable = "DATABASE_URL";
    public const string HostingModeVariable = "SWITCHKOREA_HOSTING_MODE";
    public const string RenderExternalHostnameVariable = "RENDER_EXTERNAL_HOSTNAME";

    public static ProductionRuntimeOptions From(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName);
        var protectedPath = configuration[ConnectionFileVariable];
        if (string.IsNullOrWhiteSpace(connectionString) && !string.IsNullOrWhiteSpace(protectedPath))
            connectionString = ReadProtectedConnection(protectedPath);
        var databaseUrl = configuration[DatabaseUrlVariable];
        if (string.IsNullOrWhiteSpace(connectionString) && !string.IsNullOrWhiteSpace(databaseUrl))
            connectionString = RenderPostgresConnectionString.Normalize(databaseUrl);
        return new ProductionRuntimeOptions(
            connectionString,
            configuration[DataProtectionPathVariable],
            ParseHostingMode(configuration),
            configuration[RenderExternalHostnameVariable]);
    }

    public static ProductionHostingMode ParseHostingMode(IConfiguration configuration)
    {
        var value = configuration[HostingModeVariable];
        if (string.IsNullOrWhiteSpace(value))
            return ProductionHostingMode.Windows;
        if (Enum.TryParse<ProductionHostingMode>(value, ignoreCase: true, out var mode))
            return mode;
        throw new InvalidOperationException($"{HostingModeVariable} must be Windows or Render.");
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
