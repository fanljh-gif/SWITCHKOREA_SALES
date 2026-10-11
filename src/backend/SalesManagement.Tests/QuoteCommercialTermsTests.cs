using System.Text.Json;
using System.Text.Json.Nodes;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SalesManagement.Api.Endpoints;
using SalesManagement.Api.Quotations;
using SalesManagement.Core.Repositories;
using SalesManagement.Core.Models;
using SalesManagement.Core.Quotations;
using SalesManagement.InMemory.Repositories;

namespace SalesManagement.Tests;

public sealed class QuoteCommercialTermsTests
{
    [Fact]
    public async Task Http_create_edit_and_get_preserve_camel_case_commercial_fields()
    {
        var (service, repo, manager, command) = await Fixture();
        var users = new InMemoryUserRepository();
        await users.AddAsync(manager);
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.Services.AddAuthorization();
        builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));
        builder.Services.AddSingleton<IUserRepository>(users);
        builder.Services.AddSingleton(service);
        builder.Services.AddSingleton(new QuotationDashboardService(new InMemoryInquiryRepository(), repo));
        builder.Services.AddSingleton<IQuotePdfRenderer, MigraDocQuotePdfRenderer>();
        await using var app = builder.Build();
        // Synthetic identity: verifies HTTP contracts, not real login/cookie security.
        app.Use(async (context, next) => { context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, manager.Id.ToString())], "test")); await next(); });
        app.UseAuthorization();
        app.MapQuoteEndpoints();
        app.Urls.Add("http://127.0.0.1:0");
        await app.StartAsync();
        using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
        using var created = await client.PostAsJsonAsync("/api/quotes", new {
            customerId = command.CustomerId, ownerId = manager.Id, tradeType = "domestic", currency = "KRW",
            taxType = "exclusive", validUntil = "2026-12-31", incotermCode = (string?)null, namedPlace = (string?)null,
            discountAmount = 0, shippingAmount = 0, otherAmount = 0,
            lines = new[] { new { productId = Guid.NewGuid(), description = "Switch", quantity = 1, unitPrice = 100 } },
            paymentTerms = "T/T 30%", deliveryTerms = "4 weeks", notes = "Original"
        });
        created.EnsureSuccessStatusCode();
        var json = JsonNode.Parse(await created.Content.ReadAsStringAsync())!;
        var id = json["id"]!.GetValue<string>();
        var revision = json["currentRevision"]!;
        Assert.Equal("T/T 30%", revision["paymentTerms"]!.GetValue<string>());
        var edit = revision.DeepClone().AsObject();
        edit["expectedVersion"] = revision["version"]!.GetValue<long>();
        edit["paymentTerms"] = "T/T 100%";
        edit["deliveryTerms"] = "6 weeks";
        edit["notes"] = "Updated\nSecond line";
        using var saved = await client.PutAsJsonAsync($"/api/quotes/{id}/revisions/{revision["id"]!.GetValue<string>()}", edit);
        saved.EnsureSuccessStatusCode();
        using var read = await client.GetAsync($"/api/quotes/{id}");
        read.EnsureSuccessStatusCode();
        var stored = JsonNode.Parse(await read.Content.ReadAsStringAsync())!["currentRevision"]!;
        Assert.Equal("T/T 100%", stored["paymentTerms"]!.GetValue<string>());
        Assert.Equal("6 weeks", stored["deliveryTerms"]!.GetValue<string>());
        Assert.Equal("Updated\nSecond line", stored["notes"]!.GetValue<string>());
    }

    [Fact]
    public async Task Saves_terms_and_preserves_approved_snapshot_after_new_revision_edits()
    {
        var (service, repo, manager, command) = await Fixture();
        var quote = await service.CreateAsync(WithTerms(command, "T/T 30%", "4 weeks", "Export packing\nConfirm lead time"), manager);
        var draft = quote.Revisions.Single();
        AssertTerms(draft, "T/T 30%", "4 weeks", "Export packing\nConfirm lead time");
        var pending = await service.SubmitAsync(quote.Id, draft.Id, draft.Version, manager);
        var approved = await service.ApproveAsync(quote.Id, draft.Id, pending.Version, manager);
        var next = await service.CreateRevisionAsync(quote.Id, (await repo.GetAsync(quote.Id))!.Version, manager);
        AssertTerms(next, "T/T 30%", "4 weeks", "Export packing\nConfirm lead time");
        var edit = new SaveQuoteRevisionCommand(command.TradeType, command.Currency, command.TaxType,
            command.ValidUntil, command.IncotermCode, command.NamedPlace, 0, 0, 0, command.Lines, next.Version);
        var saved = await service.SaveRevisionAsync(quote.Id, next.Id, WithTerms(edit, "T/T 100%", "6 weeks", "Updated"), manager);
        AssertTerms(saved, "T/T 100%", "6 weeks", "Updated");
        var stored = (await repo.GetAsync(quote.Id))!.Revisions.Single(x => x.Id == approved.Id);
        AssertTerms(stored, "T/T 30%", "4 weeks", "Export packing\nConfirm lead time");
        foreach (var language in new[] { QuoteLanguage.Ko, QuoteLanguage.En })
            AssertTerms(await service.GetPdfModelAsync(quote.Id, approved.Id, language, manager), "T/T 30%", "4 weeks", "Export packing\nConfirm lead time");
        await Assert.ThrowsAsync<InvalidDataException>(() => service.SaveRevisionAsync(quote.Id, approved.Id,
            WithTerms(edit with { ExpectedVersion = approved.Version }, "Changed", "Changed", "Changed"), manager));
    }

    [Theory]
    [InlineData("PaymentTerms", 501)]
    [InlineData("DeliveryTerms", 501)]
    [InlineData("Notes", 4001)]
    public async Task Rejects_overlong_terms_on_create_and_save(string field, int length)
    {
        var (service, _, manager, command) = await Fixture();
        var malformed = JsonSerializer.SerializeToNode(command)!.AsObject();
        malformed[field] = new string('x', length);
        await Assert.ThrowsAsync<InvalidDataException>(() => service.CreateAsync(malformed.Deserialize<CreateQuoteCommand>()!, manager));
        var quote = await service.CreateAsync(command, manager);
        var draft = quote.Revisions.Single();
        var edit = new SaveQuoteRevisionCommand(command.TradeType, command.Currency, command.TaxType,
            command.ValidUntil, null, null, 0, 0, 0, command.Lines, draft.Version);
        var invalidEdit = JsonSerializer.SerializeToNode(edit)!.AsObject();
        invalidEdit[field] = new string('x', length);
        await Assert.ThrowsAsync<InvalidDataException>(() => service.SaveRevisionAsync(quote.Id, draft.Id,
            invalidEdit.Deserialize<SaveQuoteRevisionCommand>()!, manager));
    }

    [Fact]
    public async Task Accepts_limits_and_normalizes_blank_terms_without_changing_internal_line_breaks()
    {
        var (service, _, manager, command) = await Fixture();
        var quote = await service.CreateAsync(WithTerms(command, new string('p', 500), new string('d', 500), new string('n', 4000)), manager);
        AssertTerms(quote.Revisions.Single(), new string('p', 500), new string('d', 500), new string('n', 4000));
        var blank = await service.CreateAsync(WithTerms(command, "  ", "\t", "  first\nsecond  "), manager);
        AssertTerms(blank.Revisions.Single(), null, null, "first\nsecond");
    }

    [Fact]
    public async Task Legacy_create_command_without_commercial_fields_remains_valid()
    {
        var (service, _, manager, command) = await Fixture();
        var quote = await service.CreateAsync(command, manager);
        AssertTerms(quote.Revisions.Single(), null, null, null);
    }

    private static T WithTerms<T>(T command, string? payment, string? delivery, string? notes)
    {
        var json = JsonSerializer.SerializeToNode(command)!.AsObject();
        json["PaymentTerms"] = payment;
        json["DeliveryTerms"] = delivery;
        json["Notes"] = notes;
        return json.Deserialize<T>()!;
    }

    private static void AssertTerms(object value, string? payment, string? delivery, string? notes)
    {
        var json = JsonSerializer.SerializeToElement(value);
        foreach (var (field, expected) in new[] { ("PaymentTerms", payment), ("DeliveryTerms", delivery), ("Notes", notes) })
        {
            Assert.True(json.TryGetProperty(field, out var actual), $"Missing {field}");
            Assert.Equal(expected, actual.GetString());
        }
    }

    private static async Task<(QuoteService Service, InMemoryQuoteRepository Repo, User Manager, CreateQuoteCommand Command)> Fixture()
    {
        var repo = new InMemoryQuoteRepository();
        var customers = new InMemoryCustomerRepository();
        var manager = new User { LoginId = "terms-manager", PasswordHash = "hash", Name = "팀장", Email = "manager@example.test", Role = Role.Manager };
        var customer = await customers.AddAsync(new Customer { Code = "TERMS", CompanyName = "Customer", Country = "KR", OwnerUserId = manager.Id });
        var service = new QuoteService(repo, customers, new InMemoryInquiryRepository());
        var command = new CreateQuoteCommand(null, customer.Id, manager.Id, QuoteTradeType.Domestic, Currency.KRW,
            QuoteTaxType.Exclusive, new(2026, 12, 31), null, null, 0, 0, 0, [new(Guid.NewGuid(), "Switch", 1, 100)]);
        return (service, repo, manager, command);
    }
}
