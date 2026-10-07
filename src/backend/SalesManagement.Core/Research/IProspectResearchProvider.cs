namespace SalesManagement.Core.Research;

public sealed record ResearchQuery(
    Guid ProfileId,
    IReadOnlyList<string> Regions,
    IReadOnlyList<string> Industries,
    IReadOnlyList<string> Keywords,
    int Limit);

public sealed record ResearchProviderResult(
    string SourceId,
    string CompanyName,
    string Country,
    string Website,
    string Industry,
    string Summary,
    string EvidenceUrl,
    DateOnly ObservedDate);

public interface IProspectResearchProvider
{
    IAsyncEnumerable<ResearchProviderResult> SearchAsync(ResearchQuery query, CancellationToken cancellationToken);
}
