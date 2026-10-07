using SalesManagement.Core.Models;
using SalesManagement.Core.Repositories;

namespace SalesManagement.InMemory.Repositories;

public sealed class InMemoryProspectCandidateRepository : InMemoryRepository<ProspectCandidate>, IProspectCandidateRepository
{
    public Task<ProspectCandidate?> FindByProviderResultAsync(string providerResultId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var value = Items.Values.SingleOrDefault(x => x.ProviderResultId == providerResultId);
        return Task.FromResult(value is null ? null : Clone(value));
    }
}

public sealed class InMemoryResearchEvidenceRepository : InMemoryRepository<ResearchEvidence>, IResearchEvidenceRepository
{
    public Task<IReadOnlyList<ResearchEvidence>> ListByCandidateAsync(Guid candidateId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ResearchEvidence>>(Items.Values.Where(x => x.ProspectCandidateId == candidateId).Select(Clone).ToArray());
}

public sealed class InMemoryProspectAssessmentRepository : InMemoryRepository<ProspectAssessment>, IProspectAssessmentRepository;

public sealed class InMemoryDuplicateMatchRepository : InMemoryRepository<ProspectDuplicateMatch>, IDuplicateMatchRepository
{
    public Task<IReadOnlyList<ProspectDuplicateMatch>> ListByCandidateAsync(Guid candidateId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ProspectDuplicateMatch>>(Items.Values.Where(x => x.ProspectCandidateId == candidateId).Select(Clone).ToArray());
}

public sealed class InMemoryProspectActivityRepository : InMemoryRepository<ProspectActivity>, IProspectActivityRepository
{
    public Task<IReadOnlyList<ProspectActivity>> ListByCandidateAsync(Guid candidateId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ProspectActivity>>(Items.Values.Where(x => x.ProspectCandidateId == candidateId).Select(Clone).ToArray());
}

public sealed class InMemoryProspectConversionRepository : InMemoryRepository<ProspectConversion>, IProspectConversionRepository
{
    public Task<ProspectConversion?> GetByCandidateAsync(Guid candidateId, CancellationToken cancellationToken = default)
    {
        var value = Items.Values.SingleOrDefault(x => x.ProspectCandidateId == candidateId);
        return Task.FromResult(value is null ? null : Clone(value));
    }
}

public sealed class InMemoryResearchTargetProfileRepository : InMemoryRepository<ResearchTargetProfile>, IResearchTargetProfileRepository;
public sealed class InMemoryResearchRunRepository : InMemoryRepository<ResearchRun>, IResearchRunRepository;
