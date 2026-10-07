namespace SalesManagement.Core.Models;

public sealed class ResearchTargetProfile : EntityBase
{
    public required string Name { get; set; }
    public List<string> Regions { get; set; } = [];
    public List<string> Industries { get; set; } = [];
    public List<string> IncludeKeywords { get; set; } = [];
    public List<string> ExcludeKeywords { get; set; } = [];
    public int MaxSearchResults { get; set; } = 100;
    public int MaxAiAnalyses { get; set; } = 40;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? NextRunAt { get; set; }
}
