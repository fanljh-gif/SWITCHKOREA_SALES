using Npgsql;

namespace SalesManagement.Postgres.Configuration;

public static class RenderPostgresConnectionString
{
    public static string Normalize(string databaseUrl)
    {
        if (!Uri.TryCreate(databaseUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != "postgres" && uri.Scheme != "postgresql")
            || string.IsNullOrWhiteSpace(uri.Host))
            throw InvalidUrl();

        var userInfoSeparator = uri.UserInfo.IndexOf(':');
        var database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/'));
        if (userInfoSeparator <= 0 || string.IsNullOrWhiteSpace(database))
            throw InvalidUrl();

        var username = Uri.UnescapeDataString(uri.UserInfo[..userInfoSeparator]);
        var password = Uri.UnescapeDataString(uri.UserInfo[(userInfoSeparator + 1)..]);
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            throw InvalidUrl();

        return new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort ? 5432 : uri.Port,
            Database = database,
            Username = username,
            Password = password,
            PersistSecurityInfo = false,
        }.ConnectionString;
    }

    private static InvalidOperationException InvalidUrl() =>
        new("DATABASE_URL must be a complete PostgreSQL URL.");
}
