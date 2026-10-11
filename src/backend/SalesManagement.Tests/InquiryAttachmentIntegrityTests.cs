using System.Text.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SalesManagement.Api.Endpoints;
using SalesManagement.Core.Repositories;
using SalesManagement.Core.Models;
using SalesManagement.Core.Quotations;
using SalesManagement.InMemory.Repositories;

namespace SalesManagement.Tests;

public sealed class InquiryAttachmentIntegrityTests
{
    [Fact]
    public async Task UploadHttpResponseReturnsMetadataWithoutFileBytes()
    {
        var user = new User { LoginId = "owner", PasswordHash = "hash", Name = "담당자", Email = "owner@example.test", Role = Role.Member };
        var users = new InMemoryUserRepository(); await users.AddAsync(user);
        var inquiries = new InMemoryInquiryRepository();
        var inquiry = await inquiries.AddAsync(new CustomerInquiry { InquiryNumber = "INQ-202610-001", Title = "문의", CustomerId = Guid.NewGuid(), CustomerName = "고객", ContactName = "담당", OwnerUserId = user.Id, ReceivedAt = new(2026, 10, 8), ResponseDueAt = new(2026, 10, 10), Summary = "사양" });
        var builder = WebApplication.CreateBuilder(); builder.Logging.ClearProviders(); builder.Services.AddAuthorization();
        builder.Services.AddSingleton<IUserRepository>(users);
        builder.Services.AddSingleton(new InquiryService(inquiries, new InMemoryCustomerRepository()));
        await using var app = builder.Build();
        // Test identity isolates response serialization; this does not test real login/cookies.
        app.Use(async (context, next) => { context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())], "test")); await next(); });
        app.UseAuthorization(); app.MapInquiryEndpoints(); app.Urls.Add("http://127.0.0.1:0"); await app.StartAsync();
        using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
        using var multipart = new MultipartFormDataContent();
        using var file = new ByteArrayContent("%PDF-1.7"u8.ToArray()); file.Headers.ContentType = new("application/pdf"); multipart.Add(file, "file", "spec.pdf");
        using var response = await client.PostAsync($"/api/inquiries/{inquiry.Id}/attachments", multipart);
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.False(document.RootElement.TryGetProperty("content", out _));
        Assert.Equal("86edbaa24831badfa0a8b04bb410141e2ee4182b6d0014493fe262a7a331c20b", document.RootElement.GetProperty("sha256").GetString());
    }

    [Fact]
    public async Task UploadPersistsSha256ForTheExactStoredBytes()
    {
        var user = new User { LoginId = "owner", PasswordHash = "hash", Name = "담당자", Email = "owner@example.test", Role = Role.Member };
        var repository = new InMemoryInquiryRepository();
        var inquiry = await repository.AddAsync(new CustomerInquiry { InquiryNumber = "INQ-202610-001", Title = "문의", CustomerId = Guid.NewGuid(), CustomerName = "고객", ContactName = "담당", OwnerUserId = user.Id, ReceivedAt = new(2026, 10, 8), ResponseDueAt = new(2026, 10, 10), Summary = "사양" });
        var service = new InquiryService(repository, new InMemoryCustomerRepository());
        var bytes = "%PDF-1.7"u8.ToArray();
        await service.AddAttachmentAsync(inquiry.Id, new("사양.pdf", "application/pdf", bytes), user);
        var stored = (await service.GetAsync(inquiry.Id, user)).Attachments.Single();
        Assert.Equal(bytes, stored.Content);
        var json = JsonSerializer.SerializeToElement(stored);
        Assert.True(json.TryGetProperty("Sha256", out var digest));
        Assert.Matches("^[0-9a-f]{64}$", digest.GetString()!);
        Assert.Equal("86edbaa24831badfa0a8b04bb410141e2ee4182b6d0014493fe262a7a331c20b", digest.GetString());
    }
}
