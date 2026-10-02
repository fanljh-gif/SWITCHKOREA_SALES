using Microsoft.Extensions.Configuration;
using Npgsql;
using SalesManagement.Api.Configuration;
using SalesManagement.Postgres.Configuration;

namespace SalesManagement.Tests;

public sealed class RenderPostgresConnectionStringTests
{
    [Fact]
    public void Normalize_decodes_render_url_into_npgsql_fields()
    {
        var normalized = RenderPostgresConnectionString.Normalize(
            "postgresql://user%40name:p%40ss%3Aword%2Fok@db.example:5433/sales%20db");

        var result = new NpgsqlConnectionStringBuilder(normalized);
        Assert.Equal("db.example", result.Host);
        Assert.Equal(5433, result.Port);
        Assert.Equal("sales db", result.Database);
        Assert.Equal("user@name", result.Username);
        Assert.Equal("p@ss:word/ok", result.Password);
        Assert.False(result.PersistSecurityInfo);
    }

    [Theory]
    [InlineData("mysql://user:supersecret@db.example/sales")]
    [InlineData("postgresql://user:supersecret@/sales")]
    [InlineData("postgresql://db.example/sales")]
    public void Normalize_rejects_invalid_urls_without_echoing_secret(string databaseUrl)
    {
        var error = Assert.Throws<InvalidOperationException>(() =>
            RenderPostgresConnectionString.Normalize(databaseUrl));

        Assert.DoesNotContain("supersecret", error.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(databaseUrl, error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Existing_dotnet_connection_string_takes_precedence_over_database_url()
    {
        var expected = "Host=preferred;Database=sales;Username=app;Password=local";
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:SalesManagement"] = expected,
            ["DATABASE_URL"] = "postgresql://render:secret@ignored.example/renderdb",
        }).Build();

        var result = ProductionRuntimeOptions.From(configuration);

        Assert.Equal(expected, result.ConnectionString);
    }
}
