namespace SalesManagement.Api.Configuration;

public sealed record ProductionRuntimeOptions(string? ConnectionString, string? DataProtectionPath)
{
    public const string ConnectionStringName = "SalesManagement";
    public const string DataProtectionPathVariable = "SWITCHKOREA_DATA_PROTECTION_PATH";

    public static ProductionRuntimeOptions From(IConfiguration configuration) => new(
        configuration.GetConnectionString(ConnectionStringName),
        configuration[DataProtectionPathVariable]);
}
