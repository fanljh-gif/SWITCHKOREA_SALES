namespace SalesManagement.Core.Models;

public sealed class ProspectCandidate : EntityBase
{
    public Guid ResearchRunId { get; set; }
    public required string ProviderResultId { get; set; }
    public required string CompanyName { get; set; }
    public string? EnglishName { get; set; }
    public required string NormalizedCompanyName { get; set; }
    public required string Country { get; set; }
    public required string Region { get; set; }
    public required string Industry { get; set; }
    public string? CompanySize { get; set; }
    public required string Domain { get; set; }
    public required string NormalizedDomain { get; set; }
    public required string Website { get; set; }
    public List<string> Products { get; set; } = [];
    public List<string> Applications { get; set; } = [];
    public Guid? OwnerUserId { get; set; }
    public ProspectStatus Status { get; set; } = ProspectStatus.Collected;
    public Guid? NetworkCompanyId { get; set; }
}
