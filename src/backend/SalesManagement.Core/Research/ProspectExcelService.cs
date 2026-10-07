using System.Collections.Concurrent;
using System.Security.Cryptography;
using ClosedXML.Excel;
using SalesManagement.Core.Auth;
using SalesManagement.Core.Models;
using SalesManagement.Core.Repositories;

namespace SalesManagement.Core.Research;

public sealed record ProspectExcelRow(int RowNumber, string CompanyName, string Country, string Website, string? Industry, string? Products, string? Notes);
public sealed record ProspectExcelValidation(Guid ValidationId, string FileHash, IReadOnlyList<ProspectExcelRow> Rows);

public sealed class ProspectExcelService(
    IProspectCandidateRepository candidates,
    IResearchEvidenceRepository evidence,
    DuplicateDetectionService duplicateDetection,
    IResearchTargetProfileRepository profiles,
    IResearchRunRepository runs)
{
    private readonly ConcurrentDictionary<Guid, ProspectExcelValidation> _validations = new();
    private readonly ConcurrentDictionary<string, Guid> _validationByHash = new(StringComparer.OrdinalIgnoreCase);

    public ProspectExcelValidation Validate(Stream stream)
    {
        using var buffer = new MemoryStream(); stream.CopyTo(buffer); var bytes = buffer.ToArray();
        var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        if (_validationByHash.TryGetValue(hash, out var existingId)) return _validations[existingId];
        using var workbook = new XLWorkbook(new MemoryStream(bytes)); var sheet = workbook.Worksheets.First();
        var headers = sheet.Row(1).CellsUsed().ToDictionary(cell => cell.GetString().Trim(), cell => cell.Address.ColumnNumber, StringComparer.OrdinalIgnoreCase);
        foreach (var required in new[] { "CompanyName", "Country", "Website" }) if (!headers.ContainsKey(required)) throw new InvalidDataException($"필수 열이 없습니다: {required}");
        string Cell(int row, string name) => headers.TryGetValue(name, out var column) ? sheet.Cell(row, column).GetString().Trim() : string.Empty;
        var rows = new List<ProspectExcelRow>();
        foreach (var row in sheet.RowsUsed().Skip(1))
        {
            var number = row.RowNumber(); var company = Cell(number, "CompanyName"); var country = Cell(number, "Country"); var website = Cell(number, "Website");
            if (string.IsNullOrWhiteSpace(company) || string.IsNullOrWhiteSpace(country) || string.IsNullOrWhiteSpace(website)) throw new InvalidDataException($"{number}행 필수값이 누락되었습니다.");
            rows.Add(new(number, company, country, website, Cell(number, "Industry"), Cell(number, "Products"), Cell(number, "Notes")));
        }
        var validation = new ProspectExcelValidation(Guid.NewGuid(), hash, rows);
        _validations[validation.ValidationId] = validation; _validationByHash[hash] = validation.ValidationId;
        return validation;
    }

    public async Task<IReadOnlyList<ProspectCandidate>> ImportAsync(Guid validationId, User actor, CancellationToken cancellationToken = default)
    {
        AuthorizationService.EnsureActive(actor);
        if (!_validations.TryGetValue(validationId, out var validation)) throw new KeyNotFoundException("Excel 검증 결과를 찾을 수 없습니다.");
        var profile = (await profiles.ListAsync(cancellationToken)).FirstOrDefault(x => x.Name == "Excel 가져오기")
            ?? await profiles.AddAsync(new ResearchTargetProfile { Name = "Excel 가져오기", IsActive = false }, cancellationToken);
        var run = (await runs.ListAsync(cancellationToken)).FirstOrDefault(x => x.InputHash == validation.FileHash)
            ?? await runs.AddAsync(new ResearchRun { ResearchTargetProfileId = profile.Id, Kind = ResearchRunKind.Excel, Status = ResearchRunStatus.Completed, InputHash = validation.FileHash, DiscoveredCount = validation.Rows.Count, EnrichedCount = validation.Rows.Count, StartedAt = DateTimeOffset.UtcNow, CompletedAt = DateTimeOffset.UtcNow }, cancellationToken);
        var imported = new List<ProspectCandidate>();
        foreach (var row in validation.Rows)
        {
            var providerId = $"excel:{validation.FileHash}:{row.RowNumber}";
            if (await candidates.FindByProviderResultAsync(providerId, cancellationToken) is { } existing) { imported.Add(existing); continue; }
            var matches = await duplicateDetection.FindMatchesAsync(new(row.CompanyName, row.Country, row.Website, row.Industry ?? string.Empty), cancellationToken);
            var domain = DuplicateDetectionService.NormalizeRootDomain(row.Website);
            var candidate = await candidates.AddAsync(new ProspectCandidate { ResearchRunId = run.Id, ProviderResultId = providerId, CompanyName = row.CompanyName, NormalizedCompanyName = DuplicateDetectionService.NormalizeCompanyName(row.CompanyName), Country = row.Country, Region = row.Country, Industry = row.Industry ?? string.Empty, Domain = domain, NormalizedDomain = domain, Website = row.Website, Products = string.IsNullOrWhiteSpace(row.Products) ? [] : row.Products.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList(), OwnerUserId = actor.Role == Role.Member ? actor.Id : null, Status = matches.Count > 0 ? ProspectStatus.DuplicateSuspected : ProspectStatus.Collected }, cancellationToken);
            await evidence.AddAsync(new ResearchEvidence { ProspectCandidateId = candidate.Id, SourceUrl = row.Website, Title = $"Excel row {row.RowNumber}", ObservedDate = DateOnly.FromDateTime(DateTime.UtcNow), Summary = row.Notes ?? "Excel 후보 등록", Confidence = EvidenceConfidence.Low, SourceHash = providerId }, cancellationToken);
            imported.Add(candidate);
        }
        return imported;
    }
}
