using SalesManagement.Core.Models;
using SalesManagement.Core.Repositories;

namespace SalesManagement.Core.Research;

public sealed class ResearchRunService(
    IResearchTargetProfileRepository profiles,
    IResearchRunRepository runs,
    IProspectCandidateRepository candidates,
    IResearchEvidenceRepository evidence,
    IDuplicateMatchRepository duplicateMatches,
    IProspectResearchProvider provider,
    DuplicateDetectionService duplicateDetection)
{
    public async Task<ResearchRun> StartAsync(ResearchRunKind kind, ResearchTargetProfile profile, string? inputHash = null, CancellationToken cancellationToken = default)
    {
        if (await profiles.GetAsync(profile.Id, cancellationToken) is null) await profiles.AddAsync(profile, cancellationToken);
        var run = await runs.AddAsync(new ResearchRun { ResearchTargetProfileId = profile.Id, Kind = kind, Status = ResearchRunStatus.Queued, InputHash = inputHash }, cancellationToken);
        return run;
    }

    public async Task<ResearchRun> ProcessAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        var run = await runs.GetAsync(runId, cancellationToken) ?? throw new KeyNotFoundException("리서치 실행을 찾을 수 없습니다.");
        var profile = await profiles.GetAsync(run.ResearchTargetProfileId, cancellationToken) ?? throw new InvalidOperationException("리서치 실행 프로필을 찾을 수 없습니다.");
        var version = run.Version;
        run.Status = ResearchRunStatus.Running; run.StartedAt ??= DateTimeOffset.UtcNow; run.FailedCount = 0; run.ErrorSummary = null;
        run = await runs.UpdateAsync(run, version, cancellationToken);
        try
        {
            var query = new ResearchQuery(profile.Id, profile.Regions, profile.Industries, profile.IncludeKeywords, profile.MaxSearchResults);
            await foreach (var result in provider.SearchAsync(query, cancellationToken).WithCancellation(cancellationToken))
            {
                run.ApiCallCount++;
                if (await candidates.FindByProviderResultAsync(result.SourceId, cancellationToken) is not null) continue;
                Validate(result);
                var draft = new ProspectCandidateDraft(result.CompanyName, result.Country, result.Website, result.Industry);
                var matches = await duplicateDetection.FindMatchesAsync(draft, cancellationToken);
                var domain = DuplicateDetectionService.NormalizeRootDomain(result.Website);
                var candidate = await candidates.AddAsync(new ProspectCandidate
                {
                    ResearchRunId = run.Id, ProviderResultId = result.SourceId, CompanyName = result.CompanyName.Trim(),
                    NormalizedCompanyName = DuplicateDetectionService.NormalizeCompanyName(result.CompanyName), Country = result.Country.Trim(),
                    Region = MapRegion(result.Country), Industry = result.Industry.Trim(), Domain = domain, NormalizedDomain = domain,
                    Website = result.Website.Trim(), Status = matches.Count > 0 ? ProspectStatus.DuplicateSuspected : ProspectStatus.ReviewPending,
                }, cancellationToken);
                await evidence.AddAsync(new ResearchEvidence { ProspectCandidateId = candidate.Id, SourceUrl = result.EvidenceUrl, Title = result.CompanyName, ObservedDate = result.ObservedDate, Summary = result.Summary, Confidence = EvidenceConfidence.Medium, SourceHash = $"provider:{result.SourceId}" }, cancellationToken);
                foreach (var match in matches)
                    await duplicateMatches.AddAsync(new ProspectDuplicateMatch { ProspectCandidateId = candidate.Id, TargetType = match.TargetType, TargetId = match.TargetId, TargetName = match.TargetName, Similarity = match.Similarity, Strength = match.Strength, Reasons = [.. match.Reasons] }, cancellationToken);
                run.DiscoveredCount++; run.EnrichedCount++;
            }
            run.Status = ResearchRunStatus.Completed; run.CompletedAt = DateTimeOffset.UtcNow;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception exception)
        {
            run.Status = ResearchRunStatus.Partial; run.FailedCount++; run.ErrorSummary = exception.Message; run.CompletedAt = DateTimeOffset.UtcNow;
        }
        return await runs.UpdateAsync(run, run.Version, cancellationToken);
    }

    public async Task<ResearchRun> RetryFailedAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        var run = await runs.GetAsync(runId, cancellationToken) ?? throw new KeyNotFoundException("리서치 실행을 찾을 수 없습니다.");
        if (run.Status is not (ResearchRunStatus.Partial or ResearchRunStatus.Failed)) return run;
        return await ProcessAsync(runId, cancellationToken);
    }

    private static void Validate(ResearchProviderResult result)
    {
        if (string.IsNullOrWhiteSpace(result.SourceId) || string.IsNullOrWhiteSpace(result.CompanyName) || string.IsNullOrWhiteSpace(result.Country)
            || string.IsNullOrWhiteSpace(result.Website) || string.IsNullOrWhiteSpace(result.EvidenceUrl) || result.ObservedDate == default)
            throw new InvalidDataException("공급자 결과에 필수 공개 근거가 없습니다.");
    }

    private static string MapRegion(string country) => country.Trim().ToUpperInvariant() switch
    {
        "KR" or "KOREA" or "대한민국" => "한국", "US" or "USA" => "미국", "CN" or "CHINA" => "중국", "IN" or "INDIA" => "인도", "VN" or "VIETNAM" => "베트남",
        "DE" or "FR" or "IT" or "ES" or "NL" or "PL" or "EU" => "유럽", _ => "동남아",
    };
}
