using SalesManagement.Core.Exceptions; using SalesManagement.Core.Models; using SalesManagement.Core.Quotations; using SalesManagement.InMemory.Repositories;
namespace SalesManagement.Tests;
public sealed class QuoteWorkflowTests
{
    [Fact] public async Task Enforces_ownership_approval_revision_and_result()
    {
        var repo = new InMemoryQuoteRepository(); var inquiryRepo = new InMemoryInquiryRepository(); var customers = new InMemoryCustomerRepository(); var owner = User(Role.Member, "member"); var other = User(Role.Member, "other"); var manager = User(Role.Manager, "manager"); var customer = await customers.AddAsync(new Customer { Code = "C1", CompanyName = "Customer", Country = "KR", DefaultCurrency = Currency.KRW, OwnerUserId = owner.Id }); var service = new QuoteService(repo, customers, inquiryRepo);
        var quote = await service.CreateAsync(new(null, customer.Id, owner.Id, QuoteTradeType.Domestic, Currency.KRW, QuoteTaxType.Exclusive, new(2026, 10, 31), null, null, 0, 0, 0, [new(Guid.NewGuid(), "Switch", 2, 100)]), owner); Assert.Matches("SWK-\\d{6}-001", quote.QuoteNumber); await Assert.ThrowsAsync<ForbiddenException>(() => service.GetAsync(quote.Id, other));
        var draft = quote.Revisions[0]; var requested = await service.SubmitAsync(quote.Id, draft.Id, draft.Version, owner); await Assert.ThrowsAsync<ForbiddenException>(() => service.ApproveAsync(quote.Id, draft.Id, requested.Version, owner)); var approved = await service.ApproveAsync(quote.Id, draft.Id, requested.Version, manager); Assert.Equal(QuoteStatus.Approved, approved.Status); var rev1 = await service.CreateRevisionAsync(quote.Id, (await repo.GetAsync(quote.Id))!.Version, owner); Assert.Equal(1, rev1.RevisionNumber);
    }
    private static User User(Role role, string login) => new() { LoginId = login, PasswordHash = "hash", Name = login, Email = $"{login}@switchkorea.com", Role = role };

    [Fact]
    public async Task PdfSnapshotIncludesValidityAndEveryStoredAmount()
    {
        var repo = new InMemoryQuoteRepository(); var customers = new InMemoryCustomerRepository(); var manager = User(Role.Manager, "pdf-manager");
        var customer = await customers.AddAsync(new Customer { Code = "PDF", CompanyName = "고객사", Country = "KR", OwnerUserId = manager.Id });
        var service = new QuoteService(repo, customers, new InMemoryInquiryRepository());
        var quote = await service.CreateAsync(new(null, customer.Id, manager.Id, QuoteTradeType.Domestic, Currency.KRW, QuoteTaxType.Exclusive, new(2026, 12, 31), null, null, 10, 5, 2, [new(Guid.NewGuid(), "스위치", 1, 100)]), manager);
        var revision = quote.Revisions.Single();
        await Assert.ThrowsAsync<InvalidDataException>(() => service.GetPdfModelAsync(quote.Id, revision.Id, QuoteLanguage.Ko, manager));
        revision = await service.SubmitAsync(quote.Id, revision.Id, revision.Version, manager);
        revision = await service.ApproveAsync(quote.Id, revision.Id, revision.Version, manager);
        var pdf = System.Text.Json.JsonSerializer.SerializeToElement(await service.GetPdfModelAsync(quote.Id, revision.Id, QuoteLanguage.Ko, manager));
        Assert.True(pdf.TryGetProperty("ValidUntil", out var validUntil));
        Assert.Equal("2026-12-31", validUntil.GetString());
        foreach (var (key, value) in new (string, decimal)[] { ("SubtotalAmount", 100), ("DiscountAmount", 10), ("ShippingAmount", 5), ("OtherAmount", 2), ("SupplyAmount", 97), ("TaxAmount", 10), ("TotalAmount", 107) })
        {
            Assert.True(pdf.TryGetProperty(key, out var actual), key);
            Assert.Equal(value, actual.GetDecimal());
        }
    }

    [Fact]
    public async Task Rejects_stale_edits_and_records_dispatch_result_with_revision_version()
    {
        var repo = new InMemoryQuoteRepository(); var customers = new InMemoryCustomerRepository(); var manager = User(Role.Manager, "manager");
        var customer = await customers.AddAsync(new Customer { Code = "C2", CompanyName = "Customer", Country = "KR", OwnerUserId = manager.Id });
        var service = new QuoteService(repo, customers, new InMemoryInquiryRepository());
        var lines = new QuoteLineInput[] { new(Guid.NewGuid(), "Switch", 1, 100) };
        var quote = await service.CreateAsync(new(null, customer.Id, manager.Id, QuoteTradeType.Domestic, Currency.KRW, QuoteTaxType.Exclusive, new(2026, 12, 31), null, null, 0, 0, 0, lines), manager);
        var draft = quote.Revisions.Single();
        var edit = new SaveQuoteRevisionCommand(QuoteTradeType.Domestic, Currency.KRW, QuoteTaxType.Exclusive, draft.ValidUntil, null, null, 0, 0, 0, lines, draft.Version);
        var saved = await service.SaveRevisionAsync(quote.Id, draft.Id, edit, manager);
        Assert.Equal(draft.Version + 1, saved.Version);
        await Assert.ThrowsAsync<ConcurrencyException>(() => service.SaveRevisionAsync(quote.Id, draft.Id, edit, manager));
        var pending = await service.SubmitAsync(quote.Id, saved.Id, saved.Version, manager);
        var approved = await service.ApproveAsync(quote.Id, saved.Id, pending.Version, manager);
        await Assert.ThrowsAsync<ConcurrencyException>(() => service.RecordDispatchAsync(quote.Id, approved.Id, new(DateTimeOffset.UtcNow, "sent", QuoteLanguage.Ko, 1), manager));
        await service.RecordDispatchAsync(quote.Id, approved.Id, new(DateTimeOffset.UtcNow, "sent", QuoteLanguage.Ko, approved.Version), manager);
        var sent = (await repo.GetAsync(quote.Id))!.Revisions.Single();
        var won = await service.RecordResultAsync(quote.Id, sent.Id, QuoteStatus.Won, "수주", sent.Version, manager);
        Assert.Equal(QuoteStatus.Won, won.Result);
    }

    [Theory]
    [InlineData(QuoteStatus.Sent)]
    [InlineData(QuoteStatus.Won)]
    [InlineData(QuoteStatus.Lost)]
    [InlineData(QuoteStatus.OnHold)]
    public async Task Creates_a_new_revision_from_every_approved_lifecycle_state(QuoteStatus state)
    {
        var repo = new InMemoryQuoteRepository(); var customers = new InMemoryCustomerRepository(); var manager = User(Role.Manager, "revision-manager");
        var customer = await customers.AddAsync(new Customer { Code = "REV", CompanyName = "Customer", Country = "KR", OwnerUserId = manager.Id });
        var service = new QuoteService(repo, customers, new InMemoryInquiryRepository());
        var quote = await service.CreateAsync(new(null, customer.Id, manager.Id, QuoteTradeType.Domestic, Currency.KRW, QuoteTaxType.Exclusive, new(2026, 12, 31), null, null, 0, 0, 0, [new(Guid.NewGuid(), "Switch", 1, 100)]), manager);
        var revision = quote.Revisions.Single();
        revision = await service.SubmitAsync(quote.Id, revision.Id, revision.Version, manager);
        revision = await service.ApproveAsync(quote.Id, revision.Id, revision.Version, manager);
        if (state != QuoteStatus.Sent)
        {
            await service.RecordDispatchAsync(quote.Id, revision.Id, new(DateTimeOffset.UtcNow, "sent", QuoteLanguage.Ko, revision.Version), manager);
            var sent = (await repo.GetAsync(quote.Id))!.Revisions.Single();
            await service.RecordResultAsync(quote.Id, sent.Id, state, state.ToString(), sent.Version, manager);
        }
        else
        {
            await service.RecordDispatchAsync(quote.Id, revision.Id, new(DateTimeOffset.UtcNow, "sent", QuoteLanguage.Ko, revision.Version), manager);
        }

        var current = (await repo.GetAsync(quote.Id))!;
        var next = await service.CreateRevisionAsync(quote.Id, current.Version, manager);

        Assert.Equal(1, next.RevisionNumber);
        Assert.Equal(QuoteStatus.Draft, next.Status);
        Assert.Null((await repo.GetAsync(quote.Id))!.Result);
        Assert.Null((await repo.GetAsync(quote.Id))!.ResultNote);
    }
}
