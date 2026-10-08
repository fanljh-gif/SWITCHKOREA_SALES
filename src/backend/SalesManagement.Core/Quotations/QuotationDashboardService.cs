using SalesManagement.Core.Auth; using SalesManagement.Core.Models; using SalesManagement.Core.Repositories;
namespace SalesManagement.Core.Quotations;
public sealed class QuotationDashboardService(IInquiryRepository inquiries, IQuoteRepository quotes)
{
    public async Task<QuotationDashboard> GetAsync(QuotationDashboardQuery query, User actor, CancellationToken ct = default)
    {
        AuthorizationService.EnsureActive(actor); var owner = actor.Role == Role.Manager ? query.OwnerId : actor.Id; var inquiryRows = (await inquiries.ListAsync(ct)).Where(x => !owner.HasValue || x.OwnerUserId == owner); var quoteRows = (await quotes.ListAsync(ct)).Where(x => !owner.HasValue || x.OwnerUserId == owner).ToArray(); var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(9)); var month = today.Month; var year = today.Year;
        var current = quoteRows.Select(q => q.Revisions.Single(r => r.Id == q.CurrentRevisionId)).ToArray(); var results = new[] { QuoteStatus.Won, QuoteStatus.Lost, QuoteStatus.OnHold }.ToDictionary(x => x.ToString(), x => quoteRows.Count(q => q.Result == x)); var decided = results[nameof(QuoteStatus.Won)] + results[nameof(QuoteStatus.Lost)];
        return new(inquiryRows.Count(x => x.Status is not InquiryStatus.Answered and not InquiryStatus.Closed), inquiryRows.Count(x => x.ResponseDueAt < today && x.Status is not InquiryStatus.Answered and not InquiryStatus.Closed), current.Count(x => x.Status == QuoteStatus.ApprovalRequested), quoteRows.Count(x => x.CreatedAt.Year == year && x.CreatedAt.Month == month), current.GroupBy(x => x.Currency).Select(g => new CurrencyAmount(g.Key, g.Sum(x => x.TotalAmount))).ToArray(), results.ToDictionary(x => char.ToLowerInvariant(x.Key[0]) + x.Key[1..], x => x.Value), decided == 0 ? 0 : Math.Round(results[nameof(QuoteStatus.Won)] * 100m / decided, 1));
    }
}
