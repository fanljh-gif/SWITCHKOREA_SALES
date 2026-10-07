using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using SalesManagement.Core.Models;
using SalesManagement.Core.Repositories;

namespace SalesManagement.Core.Research;

public sealed class DuplicateDetectionService(
    IProspectCandidateRepository candidates,
    INetworkCompanyRepository networkCompanies,
    ICustomerRepository customers)
{
    public async Task<IReadOnlyList<DuplicateMatchResult>> FindMatchesAsync(ProspectCandidateDraft draft, CancellationToken cancellationToken = default)
    {
        var domain = NormalizeRootDomain(draft.Website);
        var name = NormalizeCompanyName(draft.CompanyName);
        var country = draft.Country.Trim().ToUpperInvariant();
        var result = new List<DuplicateMatchResult>();

        foreach (var candidate in await candidates.ListAsync(cancellationToken))
        {
            if (!string.IsNullOrWhiteSpace(domain) && NormalizeRootDomain(candidate.NormalizedDomain) == domain)
                result.Add(new(DuplicateTargetType.Candidate, candidate.Id, candidate.CompanyName, DuplicateMatchStrength.Strong, 1m, ["root-domain"]));
            else if (candidate.Country.Trim().ToUpperInvariant() == country && NormalizeCompanyName(candidate.CompanyName) == name)
                result.Add(new(DuplicateTargetType.Candidate, candidate.Id, candidate.CompanyName, DuplicateMatchStrength.Strong, 1m, ["country+company-name"]));
        }

        foreach (var company in await networkCompanies.ListAsync(cancellationToken))
            if (company.Country.Trim().ToUpperInvariant() == country && NormalizeCompanyName(company.CompanyName) == name)
                result.Add(new(DuplicateTargetType.NetworkCompany, company.Id, company.CompanyName, DuplicateMatchStrength.Suspected, .9m, ["country+similar-company-name"]));

        foreach (var customer in await customers.ListAsync(cancellationToken))
            if (customer.Country.Trim().ToUpperInvariant() == country && NormalizeCompanyName(customer.CompanyName) == name)
                result.Add(new(DuplicateTargetType.Customer, customer.Id, customer.CompanyName, DuplicateMatchStrength.Suspected, .9m, ["country+similar-company-name"]));

        return result.GroupBy(x => (x.TargetType, x.TargetId)).Select(x => x.First()).ToArray();
    }

    public static string NormalizeRootDomain(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var candidate = value.Contains("://", StringComparison.Ordinal) ? value : $"https://{value}";
        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri)) return string.Empty;
        var host = new IdnMapping().GetAscii(uri.Host.TrimEnd('.')).ToLowerInvariant();
        if (host.StartsWith("www.", StringComparison.Ordinal)) host = host[4..];
        var labels = host.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (labels.Length <= 2) return host;
        var useThree = labels[^1].Length == 2 && new[] { "co", "com", "org", "net" }.Contains(labels[^2]);
        return string.Join('.', labels.TakeLast(useThree ? 3 : 2));
    }

    public static string NormalizeCompanyName(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormKC).ToLowerInvariant();
        normalized = Regex.Replace(normalized, @"\b(gmbh|ag|incorporated|inc|limited|ltd|llc|corp|corporation|co)\b", string.Empty, RegexOptions.CultureInvariant);
        return Regex.Replace(normalized, @"[^\p{L}\p{N}]", string.Empty, RegexOptions.CultureInvariant);
    }
}
