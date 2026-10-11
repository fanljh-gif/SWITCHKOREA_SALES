using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SalesManagement.Api.Configuration;
using SalesManagement.Api.Endpoints;
using SalesManagement.Api.Quotations;
using SalesManagement.Core.Auth;
using SalesManagement.Core.Models;
using SalesManagement.Core.Quotations;
using SalesManagement.Core.Repositories;
using SalesManagement.InMemory.Repositories;

namespace SalesManagement.Tests;

public sealed class QuoteLoginIntegrationTests
{
    [Fact]
    public async Task Real_login_cookie_enforces_quote_ownership_approval_and_logout()
    {
        var users = new InMemoryUserRepository();
        var hasher = new PasswordHasher<User>();
        var password = Guid.NewGuid().ToString("N") + "!aA1";
        var actors = new[] { Role.Manager, Role.Member, Role.Member }.Select((role, i) => new User {
            LoginId = $"qa-{i}", Name = $"QA {i}", Email = $"qa-{i}@example.test", Role = role, PasswordHash = ""
        }).ToArray();
        foreach (var actor in actors) { actor.PasswordHash = hasher.HashPassword(actor, password); await users.AddAsync(actor); }
        var customers = new InMemoryCustomerRepository();
        var customer = await customers.AddAsync(new Customer { Code = "LOGIN-QA", CompanyName = "Test only", Country = "KR", OwnerUserId = actors[1].Id });
        var quotes = new InMemoryQuoteRepository();
        var inquiries = new InMemoryInquiryRepository();
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.Services.AddSalesCookieAuthentication();
        builder.Services.AddAuthorization();
        builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));
        builder.Services.AddSingleton<IUserRepository>(users);
        builder.Services.AddSingleton(new AuthService(users, hasher, TimeProvider.System, AuthOptions.Default));
        builder.Services.AddSingleton(new QuoteService(quotes, customers, inquiries));
        builder.Services.AddSingleton(new QuotationDashboardService(inquiries, quotes));
        builder.Services.AddSingleton<IQuotePdfRenderer, MigraDocQuotePdfRenderer>();
        await using var app = builder.Build();
        app.Use(async (context, next) => {
            try { await next(); }
            catch (Exception e) when (NetworkProblemMapper.CanMap(e)) {
                var problem = NetworkProblemMapper.Map(e);
                context.Response.StatusCode = problem.Status;
                await context.Response.WriteAsJsonAsync(new { problem.Title, problem.Status });
            }
        });
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapAuthEndpoints();
        app.MapQuoteEndpoints();
        app.Urls.Add("http://127.0.0.1:0");
        await app.StartAsync();
        using var client = new HttpClient(new SocketsHttpHandler { UseCookies = false, AllowAutoRedirect = false }) { BaseAddress = new Uri(app.Urls.Single()) };
        using var anonymous = await client.GetAsync("/api/quotes");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        using var badLogin = await client.PostAsJsonAsync("/api/auth/login", new { loginId = actors[1].LoginId, password = "incorrect" });
        Assert.Equal(HttpStatusCode.Unauthorized, badLogin.StatusCode);
        Assert.False(badLogin.Headers.Contains("Set-Cookie"));

        // Manually replay the real cookie over loopback HTTP; this does not validate HTTPS/browser transport.
        async Task<string> Login(User actor) {
            using var response = await client.PostAsJsonAsync("/api/auth/login", new { loginId = actor.LoginId, password });
            response.EnsureSuccessStatusCode();
            var header = Assert.Single(response.Headers.GetValues("Set-Cookie"));
            Assert.Contains("secure", header, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("httponly", header, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("samesite=strict", header, StringComparison.OrdinalIgnoreCase);
            return header.Split(';')[0];
        }
        var memberCookie = await Login(actors[1]);
        var otherCookie = await Login(actors[2]);
        var managerCookie = await Login(actors[0]);
        async Task<HttpResponseMessage> Send(HttpMethod method, string path, string cookie, object? body = null) {
            using var request = new HttpRequestMessage(method, path);
            request.Headers.Add("Cookie", cookie);
            if (body is not null) request.Content = JsonContent.Create(body);
            return await client.SendAsync(request);
        }
        using var me = await Send(HttpMethod.Get, "/api/auth/me", memberCookie);
        me.EnsureSuccessStatusCode();
        Assert.Equal(actors[1].Id.ToString(), JsonNode.Parse(await me.Content.ReadAsStringAsync())!["id"]!.GetValue<string>());
        using var created = await Send(HttpMethod.Post, "/api/quotes", memberCookie, new {
            customerId = customer.Id, ownerId = actors[1].Id, tradeType = "domestic", currency = "KRW", taxType = "exclusive",
            validUntil = "2026-12-31", lines = new[] { new { productId = Guid.NewGuid(), description = "QA switch", quantity = 1, unitPrice = 100 } }
        });
        created.EnsureSuccessStatusCode();
        var quote = JsonNode.Parse(await created.Content.ReadAsStringAsync())!;
        var path = $"/api/quotes/{quote["id"]!.GetValue<string>()}";
        var revision = quote["currentRevision"]!;
        var revisionPath = $"{path}/revisions/{revision["id"]!.GetValue<string>()}";
        using var otherRead = await Send(HttpMethod.Get, path, otherCookie);
        Assert.Equal(HttpStatusCode.Forbidden, otherRead.StatusCode);
        using var submitted = await Send(HttpMethod.Post, revisionPath + "/submit", memberCookie, new { expectedVersion = revision["version"]!.GetValue<long>() });
        submitted.EnsureSuccessStatusCode();
        var pending = JsonNode.Parse(await submitted.Content.ReadAsStringAsync())!;
        var approvalBody = new { expectedVersion = pending["version"]!.GetValue<long>() };
        using var memberApproval = await Send(HttpMethod.Post, revisionPath + "/approve", memberCookie, approvalBody);
        Assert.Equal(HttpStatusCode.Forbidden, memberApproval.StatusCode);
        using var managerApproval = await Send(HttpMethod.Post, revisionPath + "/approve", managerCookie, approvalBody);
        managerApproval.EnsureSuccessStatusCode();
        Assert.Equal("approved", JsonNode.Parse(await managerApproval.Content.ReadAsStringAsync())!["status"]!.GetValue<string>());
        using var pdf = await Send(HttpMethod.Get, revisionPath + "/pdf?language=ko", memberCookie);
        pdf.EnsureSuccessStatusCode();
        using var afterPdf = await Send(HttpMethod.Get, path, memberCookie);
        afterPdf.EnsureSuccessStatusCode();
        var generation = Assert.Single(JsonNode.Parse(await afterPdf.Content.ReadAsStringAsync())!["currentRevision"]!["dispatches"]!.AsArray());
        Assert.NotNull(generation!["pdfHash"]?.GetValue<string>());
        Assert.Equal(actors[1].Id.ToString(), generation["recordedBy"]!.GetValue<string>());
        using var logout = await Send(HttpMethod.Post, "/api/auth/logout", memberCookie);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Contains("expires=", Assert.Single(logout.Headers.GetValues("Set-Cookie")), StringComparison.OrdinalIgnoreCase);
        using var afterLogout = await client.GetAsync("/api/quotes");
        Assert.Equal(HttpStatusCode.Unauthorized, afterLogout.StatusCode);
    }
}
