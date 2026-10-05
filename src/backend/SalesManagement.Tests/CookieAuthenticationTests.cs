using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SalesManagement.Api.Configuration;

namespace SalesManagement.Tests;

public sealed class CookieAuthenticationTests
{
    [Fact]
    public async Task Api_authentication_failures_return_status_codes_without_redirects()
    {
        await using var app = await StartAppAsync();
        using var client = ClientFor(app);

        using var unauthorized = await client.GetAsync("/api/private");
        Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);
        Assert.Null(unauthorized.Headers.Location);

        using var signIn = await client.PostAsync("/sign-in", null);
        var cookie = signIn.Headers.GetValues("Set-Cookie").Single().Split(';')[0];
        using var forbiddenRequest = new HttpRequestMessage(HttpMethod.Get, "/api/manager");
        forbiddenRequest.Headers.Add("Cookie", cookie);
        using var forbidden = await client.SendAsync(forbiddenRequest);

        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        Assert.Null(forbidden.Headers.Location);
    }

    private static async Task<WebApplication> StartAppAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddSalesCookieAuthentication();
        builder.Services.AddAuthorizationBuilder()
            .AddPolicy("manager", policy => policy.RequireRole("manager"));
        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapGet("/api/private", () => Results.Ok()).RequireAuthorization();
        app.MapGet("/api/manager", () => Results.Ok()).RequireAuthorization("manager");
        app.MapPost("/sign-in", async (HttpContext context) =>
        {
            var identity = new ClaimsIdentity(
                [new Claim(ClaimTypes.Name, "member"), new Claim(ClaimTypes.Role, "member")],
                CookieAuthenticationDefaults.AuthenticationScheme);
            await context.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity));
        });
        await app.StartAsync();
        return app;
    }

    private static HttpClient ClientFor(WebApplication app)
    {
        var address = app.Services.GetRequiredService<IServer>().Features
            .Get<IServerAddressesFeature>()!.Addresses.Single();
        return new HttpClient(new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            UseCookies = false,
        })
        {
            BaseAddress = new Uri(address),
        };
    }
}
