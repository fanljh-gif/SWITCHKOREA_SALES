using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using SalesManagement.Api.Configuration;
using SalesManagement.Core.Repositories;
using SalesManagement.InMemory.Repositories;

namespace SalesManagement.Tests;

public sealed class ProductionRuntimeTests
{
    [Fact]
    public void Production_requires_a_PostgreSql_connection_string()
    {
        var services = new ServiceCollection();
        var configuration = Configuration(new Dictionary<string, string?>
        {
            ["SWITCHKOREA_DATA_PROTECTION_PATH"] = Path.GetTempPath(),
        });

        var error = Assert.Throws<InvalidOperationException>(() =>
            services.AddSalesProductionRuntime(configuration, Environment(Environments.Production)));

        Assert.Contains("ConnectionStrings:SalesManagement", error.Message);
    }

    [Fact]
    public void Development_without_a_connection_uses_in_memory_repositories()
    {
        var services = new ServiceCollection();

        services.AddSalesProductionRuntime(Configuration([]), Environment(Environments.Development));

        using var provider = services.BuildServiceProvider();
        Assert.IsType<InMemoryUserRepository>(provider.GetRequiredService<IUserRepository>());
    }

    [Fact]
    public async Task Production_readiness_is_unhealthy_when_PostgreSql_is_unreachable()
    {
        var keyPath = Path.Combine(Path.GetTempPath(), $"switch-korea-keys-{Guid.NewGuid():N}");
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSalesProductionRuntime(Configuration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:SalesManagement"] = "Host=127.0.0.1;Port=1;Database=missing;Username=missing;Timeout=1",
            ["SWITCHKOREA_DATA_PROTECTION_PATH"] = keyPath,
        }), Environment(Environments.Production));

        using var provider = services.BuildServiceProvider();
        var result = await provider.GetRequiredService<HealthCheckService>()
            .CheckHealthAsync(x => x.Tags.Contains("ready"));

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    [Fact]
    public void Production_persists_and_encrypts_data_protection_keys()
    {
        var keyPath = Path.Combine(Path.GetTempPath(), $"switch-korea-keys-{Guid.NewGuid():N}");
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSalesProductionRuntime(Configuration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:SalesManagement"] = "Host=127.0.0.1;Port=1;Database=missing;Username=missing;Timeout=1",
            ["SWITCHKOREA_DATA_PROTECTION_PATH"] = keyPath,
        }), Environment(Environments.Production));

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<KeyManagementOptions>>().Value;

        var repository = Assert.IsType<FileSystemXmlRepository>(options.XmlRepository);
        Assert.Equal(Path.GetFullPath(keyPath), repository.Directory.FullName, ignoreCase: true);
        Assert.NotNull(options.XmlEncryptor);
    }

    [Fact]
    public void Production_loads_a_machine_protected_connection_file()
    {
        if (!OperatingSystem.IsWindows())
            return;
        var path = Path.Combine(Path.GetTempPath(), $"switch-korea-connection-{Guid.NewGuid():N}.dpapi");
        var plain = System.Text.Encoding.UTF8.GetBytes("Host=127.0.0.1;Port=1;Database=missing;Username=missing;Timeout=1");
        try
        {
            File.WriteAllBytes(path, ProtectedData.Protect(plain, null, DataProtectionScope.LocalMachine));
            var services = new ServiceCollection();
            services.AddSalesProductionRuntime(Configuration(new Dictionary<string, string?>
            {
                ["SWITCHKOREA_CONNECTION_FILE"] = path,
                ["SWITCHKOREA_DATA_PROTECTION_PATH"] = Path.Combine(Path.GetTempPath(), $"switch-korea-keys-{Guid.NewGuid():N}"),
            }), Environment(Environments.Production));

            using var provider = services.BuildServiceProvider();
            Assert.NotNull(provider.GetService<SalesManagement.Postgres.SalesDbContext>());
        }
        finally
        {
            Array.Clear(plain);
            File.Delete(path);
        }
    }

    private static IConfiguration Configuration(IEnumerable<KeyValuePair<string, string?>> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    private static IWebHostEnvironment Environment(string name) => new TestEnvironment { EnvironmentName = name };

    private sealed class TestEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "SalesManagement.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = Path.GetTempPath();
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
