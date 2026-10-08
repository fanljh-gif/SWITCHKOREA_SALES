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
}
