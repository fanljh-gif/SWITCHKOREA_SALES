using SalesManagement.Core.Models;

namespace SalesManagement.Core.Repositories;

public interface IProspectCandidateRepository : IRepository<ProspectCandidate>
{
    Task<ProspectCandidate?> FindByProviderResultAsync(string providerResultId, CancellationToken cancellationToken = default);
}

public interface IResearchEvidenceRepository : IRepository<ResearchEvidence>
{
    Task<IReadOnlyList<ResearchEvidence>> ListByCandidateAsync(Guid candidateId, CancellationToken cancellationToken = default);
}

public interface IProspectAssessmentRepository : IRepository<ProspectAssessment>;

public interface IDuplicateMatchRepository : IRepository<ProspectDuplicateMatch>
{
    Task<IReadOnlyList<ProspectDuplicateMatch>> ListByCandidateAsync(Guid candidateId, CancellationToken cancellationToken = default);
}

public interface IProspectActivityRepository : IRepository<ProspectActivity>
{
    Task<IReadOnlyList<ProspectActivity>> ListByCandidateAsync(Guid candidateId, CancellationToken cancellationToken = default);
}

public interface IProspectConversionRepository : IRepository<ProspectConversion>
{
    Task<ProspectConversion?> GetByCandidateAsync(Guid candidateId, CancellationToken cancellationToken = default);
}

public interface IResearchTargetProfileRepository : IRepository<ResearchTargetProfile>;
public interface IResearchRunRepository : IRepository<ResearchRun>;
