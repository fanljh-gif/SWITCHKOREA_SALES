using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SalesManagement.Api.Configuration;

namespace SalesManagement.Tests;

public sealed class RenderProxyTests
{
    [Fact]
    public async Task Render_forwarded_https_request_does_not_redirect()
    {
        await using var app = await StartAppAsync();
        using var client = ClientFor(app);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Host = "switch-korea-sales-test.onrender.com";
        request.Headers.Add("X-Forwarded-For", "198.51.100.42");
        request.Headers.Add("X-Forwarded-Proto", "https");

        using var response = await client.SendAsync(request);

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Untrusted_host_is_rejected()
    {
        await using var app = await StartAppAsync();
        using var client = ClientFor(app);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Host = "attacker.example";
        request.Headers.Add("X-Forwarded-For", "198.51.100.42");
        request.Headers.Add("X-Forwarded-Proto", "https");

        using var response = await client.SendAsync(request);

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task<WebApplication> StartAppAsync()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["SWITCHKOREA_HOSTING_MODE"] = "Render",
            ["RENDER_EXTERNAL_HOSTNAME"] = "switch-korea-sales-test.onrender.com",
        }).Build();
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddSalesProxyRuntime(configuration, new TestEnvironment());
        var app = builder.Build();
        app.UseSalesProxyRuntime(new TestEnvironment());
        app.UseSalesHttpsPolicy(new TestEnvironment());
        app.MapGet("/health", () => Results.Ok());
        await app.StartAsync();
        return app;
    }

    private static HttpClient ClientFor(WebApplication app)
    {
        var address = app.Services.GetRequiredService<IServer>().Features
            .Get<IServerAddressesFeature>()!.Addresses.Single();
        return new HttpClient(new SocketsHttpHandler { AllowAutoRedirect = false })
        {
            BaseAddress = new Uri(address),
        };
    }

    private sealed class TestEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "SalesManagement.Tests";
        public Microsoft.Extensions.FileProviders.IFileProvider WebRootFileProvider { get; set; } = null!;
        public string WebRootPath { get; set; } = Path.GetTempPath();
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
