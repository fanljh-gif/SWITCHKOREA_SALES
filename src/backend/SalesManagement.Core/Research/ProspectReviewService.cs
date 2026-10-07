using System.Text.Json;
using SalesManagement.Core.Auth;
using SalesManagement.Core.Exceptions;
using SalesManagement.Core.Models;
using SalesManagement.Core.Network;
using SalesManagement.Core.Repositories;

namespace SalesManagement.Core.Research;

public sealed class ProspectReviewService(
    IProspectCandidateRepository candidates,
    IResearchEvidenceRepository evidence,
    IDuplicateMatchRepository duplicateMatches,
    IProspectActivityRepository activities,
    IProspectConversionRepository conversions,
    NetworkCompanyService networkCompanies)
{
    public async Task<ProspectCandidate> AssignAsync(Guid candidateId, Guid ownerUserId, long expectedVersion, User actor, CancellationToken cancellationToken = default)
    {
        AuthorizationService.EnsureManager(actor);
        var candidate = await RequireCandidate(candidateId, cancellationToken);
        candidate.OwnerUserId = ownerUserId;
        var saved = await candidates.UpdateAsync(candidate, expectedVersion, cancellationToken);
        await AddActivity(saved, actor, ProspectActivityAction.Assigned, new { ownerUserId }, cancellationToken);
        return saved;
    }

    public async Task<ProspectCandidate> ReviewAsync(Guid candidateId, ProspectReviewDecision decision, long expectedVersion, User actor, string? note = null, CancellationToken cancellationToken = default)
    {
        AuthorizationService.EnsureActive(actor);
        var candidate = await RequireCandidate(candidateId, cancellationToken);
        if (actor.Role != Role.Manager && candidate.OwnerUserId != actor.Id) throw new ForbiddenException("본인 담당 후보만 검토할 수 있습니다.");
        if (decision == ProspectReviewDecision.Approve) throw new NetworkValidationException("승인은 영업망 전환 절차를 사용해 주세요.");
        candidate.Status = decision switch
        {
            ProspectReviewDecision.OnHold => ProspectStatus.OnHold,
            ProspectReviewDecision.Exclude or ProspectReviewDecision.Duplicate => ProspectStatus.Excluded,
            ProspectReviewDecision.Distinct => ProspectStatus.ReviewPending,
            _ => candidate.Status,
        };
        foreach (var match in await duplicateMatches.ListByCandidateAsync(candidateId, cancellationToken))
        {
            if (decision is not (ProspectReviewDecision.Distinct or ProspectReviewDecision.Duplicate)) continue;
            match.Decision = decision == ProspectReviewDecision.Distinct ? DuplicateDecision.Distinct : DuplicateDecision.Duplicate;
            await duplicateMatches.UpdateAsync(match, match.Version, cancellationToken);
        }
        var saved = await candidates.UpdateAsync(candidate, expectedVersion, cancellationToken);
        await AddActivity(saved, actor, decision == ProspectReviewDecision.Exclude ? ProspectActivityAction.Excluded : ProspectActivityAction.Reviewed, new { decision, note }, cancellationToken);
        return saved;
    }

    public async Task<ProspectConversionResult> ApproveAndConvertAsync(Guid candidateId, long expectedVersion, User actor, CancellationToken cancellationToken = default)
    {
        AuthorizationService.EnsureManager(actor);
        if (await conversions.GetByCandidateAsync(candidateId, cancellationToken) is { } existing)
        {
            var existingCandidate = await RequireCandidate(candidateId, cancellationToken);
            var existingNetwork = await networkCompanies.GetAsync(existing.NetworkCompanyId, cancellationToken) ?? throw new KeyNotFoundException("전환된 영업망 회사를 찾을 수 없습니다.");
            return new(existingCandidate, existingNetwork, existing);
        }

        var candidate = await RequireCandidate(candidateId, cancellationToken);
        var sources = await evidence.ListByCandidateAsync(candidateId, cancellationToken);
        if (sources.Count == 0 || sources.Any(x => string.IsNullOrWhiteSpace(x.SourceUrl) || x.ObservedDate == default))
            throw new NetworkValidationException("출처 URL과 조사일이 있는 근거가 필요합니다.");
        if ((await duplicateMatches.ListByCandidateAsync(candidateId, cancellationToken)).Any(x => x.Decision == DuplicateDecision.Pending))
            throw new NetworkValidationException("중복 후보 판정을 먼저 완료해 주세요.");
        var ownerId = candidate.OwnerUserId ?? actor.Id;
        var network = await networkCompanies.SaveCompanyAsync(new(null, candidate.CompanyName, candidate.Country, candidate.Industry, LifecycleType.Prospect, false, ownerId, EntityStatus.Active), actor, 0, cancellationToken);
        candidate.Status = ProspectStatus.Converted;
        candidate.NetworkCompanyId = network.Id;
        var savedCandidate = await candidates.UpdateAsync(candidate, expectedVersion, cancellationToken);
        var conversion = await conversions.AddAsync(new ProspectConversion { ProspectCandidateId = candidate.Id, NetworkCompanyId = network.Id, ApprovedByUserId = actor.Id }, cancellationToken);
        await AddActivity(savedCandidate, actor, ProspectActivityAction.Converted, new { networkCompanyId = network.Id }, cancellationToken);
        return new(savedCandidate, network, conversion);
    }

    private async Task<ProspectCandidate> RequireCandidate(Guid id, CancellationToken cancellationToken) =>
        await candidates.GetAsync(id, cancellationToken) ?? throw new KeyNotFoundException("잠재고객 후보를 찾을 수 없습니다.");

    private Task AddActivity(ProspectCandidate candidate, User actor, ProspectActivityAction action, object after, CancellationToken cancellationToken) =>
        activities.AddAsync(new ProspectActivity { ProspectCandidateId = candidate.Id, ActorUserId = actor.Id, Action = action, AfterJson = JsonSerializer.Serialize(after) }, cancellationToken);
}
