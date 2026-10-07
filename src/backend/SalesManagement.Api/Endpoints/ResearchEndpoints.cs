using System.Security.Claims;
using SalesManagement.Core.Auth;
using SalesManagement.Core.Models;
using SalesManagement.Core.Repositories;
using SalesManagement.Core.Research;

namespace SalesManagement.Api.Endpoints;

public static class ResearchEndpoints
{
    public static IEndpointRouteBuilder MapResearchEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/research").RequireAuthorization();

        group.MapGet("/summary", async (IResearchTargetProfileRepository profiles, IResearchRunRepository runs, IProspectCandidateRepository candidates, CancellationToken ct) =>
        {
            var profile = (await profiles.ListAsync(ct)).FirstOrDefault();
            var runRows = await runs.ListAsync(ct); var candidateRows = await candidates.ListAsync(ct);
            return Results.Ok(new { statusCounts = candidateRows.GroupBy(x => x.Status).ToDictionary(x => x.Key, x => x.Count()), monthlyUsage = new { apiCallCount = runRows.Sum(x => x.ApiCallCount), costKrw = runRows.Sum(x => x.CostKrw), limitKrw = 0, percent = 0 }, targetProfile = profile, recentRuns = runRows.OrderByDescending(x => x.CreatedAt).Take(10) });
        });
        group.MapPut("/profiles", async (SaveProfileRequest request, IResearchTargetProfileRepository profiles, IUserRepository users, HttpContext context, CancellationToken ct) =>
        {
            AuthorizationService.EnsureManager(await Actor(context, users, ct));
            var profile = new ResearchTargetProfile { Id = request.Id ?? Guid.NewGuid(), Name = request.Name, Regions = request.Regions, Industries = request.Industries, IncludeKeywords = request.IncludeKeywords, ExcludeKeywords = request.ExcludeKeywords, MaxSearchResults = request.MaxSearchResults, MaxAiAnalyses = request.MaxAiAnalyses, IsActive = request.IsActive, NextRunAt = request.NextRunAt };
            return Results.Ok(request.Id.HasValue ? await profiles.UpdateAsync(profile, request.ExpectedVersion, ct) : await profiles.AddAsync(profile, ct));
        });
        group.MapPost("/runs", async (StartRunRequest request, IResearchTargetProfileRepository profiles, ResearchRunService service, IUserRepository users, HttpContext context, CancellationToken ct) =>
        {
            var actor = await Actor(context, users, ct); if (request.Kind == ResearchRunKind.Manual) AuthorizationService.EnsureManager(actor);
            var profile = request.ProfileId.HasValue ? await profiles.GetAsync(request.ProfileId.Value, ct) : (await profiles.ListAsync(ct)).FirstOrDefault();
            if (profile is null) return Results.UnprocessableEntity(new { title = "활성 리서치 프로필이 필요합니다.", status = 422 });
            return Results.Ok(await service.StartAsync(request.Kind, profile, cancellationToken: ct));
        });
        group.MapGet("/candidates", async (string? search, string? region, string? industry, ProspectStatus? status, Guid? ownerId, Guid? runId, IProspectCandidateRepository candidates, CancellationToken ct) =>
        {
            var rows = await candidates.ListAsync(ct);
            return Results.Ok(rows.Where(x => (string.IsNullOrWhiteSpace(search) || $"{x.CompanyName} {x.Country} {x.Domain}".Contains(search, StringComparison.OrdinalIgnoreCase)) && (string.IsNullOrWhiteSpace(region) || x.Region == region) && (string.IsNullOrWhiteSpace(industry) || x.Industry == industry) && (!status.HasValue || x.Status == status) && (!ownerId.HasValue || x.OwnerUserId == ownerId) && (!runId.HasValue || x.ResearchRunId == runId)).Select(ToCandidate));
        });
        group.MapGet("/candidates/{id:guid}", async (Guid id, IProspectCandidateRepository candidates, IResearchEvidenceRepository evidence, IDuplicateMatchRepository duplicates, IProspectAssessmentRepository assessments, CancellationToken ct) =>
        {
            var candidate = await candidates.GetAsync(id, ct); if (candidate is null) return Results.NotFound();
            return Results.Ok(await ToCandidateDetail(candidate, evidence, duplicates, assessments, ct));
        });
        group.MapPost("/candidates/{id:guid}/assign", async (Guid id, AssignRequest request, ProspectReviewService service, IUserRepository users, HttpContext context, CancellationToken ct) => Results.Ok(ToCandidate(await service.AssignAsync(id, request.OwnerId, request.ExpectedVersion, await Actor(context, users, ct), ct))));
        group.MapPost("/candidates/{id:guid}/review", async (Guid id, ReviewRequest request, ProspectReviewService service, IUserRepository users, HttpContext context, CancellationToken ct) => Results.Ok(ToCandidate(await service.ReviewAsync(id, request.Decision, request.ExpectedVersion, await Actor(context, users, ct), request.Note, ct))));
        group.MapPost("/candidates/{id:guid}/approve", async (Guid id, ApproveRequest request, ProspectReviewService service, IUserRepository users, HttpContext context, CancellationToken ct) => Results.Ok(ToCandidate((await service.ApproveAndConvertAsync(id, request.ExpectedVersion, await Actor(context, users, ct), ct)).Candidate)));
        group.MapGet("/candidates/{id:guid}/duplicates", async (Guid id, IDuplicateMatchRepository duplicates, CancellationToken ct) => Results.Ok(await duplicates.ListByCandidateAsync(id, ct)));
        group.MapPost("/imports/validate", async (IFormFile file, ProspectExcelService service) =>
        {
            if (file.Length is 0 or > 5_242_880 || !Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase)) return Results.UnprocessableEntity(new { title = "5MB 이하 XLSX 파일만 업로드할 수 있습니다.", status = 422 });
            await using var stream = file.OpenReadStream(); return Results.Ok(service.Validate(stream));
        }).DisableAntiforgery();
        group.MapPost("/imports/{id:guid}/apply", async (Guid id, ProspectExcelService service, IUserRepository users, HttpContext context, CancellationToken ct) => Results.Ok((await service.ImportAsync(id, await Actor(context, users, ct), ct)).Select(ToCandidate)));
        return endpoints;
    }

    private static object ToCandidate(ProspectCandidate x) => new { x.Id, x.CompanyName, x.EnglishName, x.Country, x.Region, x.Industry, x.CompanySize, x.Domain, x.Website, x.Products, x.Applications, ownerId = x.OwnerUserId, x.Status, runId = x.ResearchRunId, x.NetworkCompanyId, x.Version };
    private static async Task<object> ToCandidateDetail(ProspectCandidate x, IResearchEvidenceRepository evidence, IDuplicateMatchRepository duplicates, IProspectAssessmentRepository assessments, CancellationToken ct)
    {
        var score = (await assessments.ListAsync(ct)).Where(a => a.ProspectCandidateId == x.Id).OrderByDescending(a => a.AssessedAt).FirstOrDefault();
        return new { x.Id, x.CompanyName, x.EnglishName, x.Country, x.Region, x.Industry, x.CompanySize, x.Domain, x.Website, x.Products, x.Applications, ownerId = x.OwnerUserId, x.Status, runId = x.ResearchRunId, x.NetworkCompanyId, x.Version, evidence = await evidence.ListByCandidateAsync(x.Id, ct), duplicateMatches = await duplicates.ListByCandidateAsync(x.Id, ct), score };
    }
    private static async Task<User> Actor(HttpContext context, IUserRepository users, CancellationToken ct)
    {
        var value = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(value, out var id) || await users.GetAsync(id, ct) is not { } user) throw new UnauthorizedAccessException();
        return user;
    }

    public sealed record SaveProfileRequest(Guid? Id, string Name, List<string> Regions, List<string> Industries, List<string> IncludeKeywords, List<string> ExcludeKeywords, int MaxSearchResults, int MaxAiAnalyses, bool IsActive, DateTimeOffset? NextRunAt, long ExpectedVersion);
    public sealed record StartRunRequest(ResearchRunKind Kind, Guid? ProfileId);
    public sealed record AssignRequest(Guid OwnerId, long ExpectedVersion);
    public sealed record ReviewRequest(ProspectReviewDecision Decision, long ExpectedVersion, string? Note);
    public sealed record ApproveRequest(long ExpectedVersion);
}
