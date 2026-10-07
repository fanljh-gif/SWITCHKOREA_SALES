using Microsoft.EntityFrameworkCore;
using SalesManagement.Core.Models;
using SalesManagement.Core.Repositories;

namespace SalesManagement.Postgres.Repositories;

public sealed class ProspectCandidateRepository(SalesDbContext db) : EfRepository<ProspectCandidate>(db), IProspectCandidateRepository
{
    public Task<ProspectCandidate?> FindByProviderResultAsync(string id, CancellationToken ct = default) => Db.ProspectCandidates.AsNoTracking().SingleOrDefaultAsync(x => x.ProviderResultId == id, ct);
}
public sealed class ResearchEvidenceRepository(SalesDbContext db) : EfRepository<ResearchEvidence>(db), IResearchEvidenceRepository
{
    public async Task<IReadOnlyList<ResearchEvidence>> ListByCandidateAsync(Guid id, CancellationToken ct = default) => await Db.ResearchEvidence.AsNoTracking().Where(x => x.ProspectCandidateId == id).OrderBy(x => x.ObservedDate).ToArrayAsync(ct);
}
public sealed class ProspectAssessmentRepository(SalesDbContext db) : EfRepository<ProspectAssessment>(db), IProspectAssessmentRepository;
public sealed class DuplicateMatchRepository(SalesDbContext db) : EfRepository<ProspectDuplicateMatch>(db), IDuplicateMatchRepository
{
    public async Task<IReadOnlyList<ProspectDuplicateMatch>> ListByCandidateAsync(Guid id, CancellationToken ct = default) => await Db.ProspectDuplicateMatches.AsNoTracking().Where(x => x.ProspectCandidateId == id).ToArrayAsync(ct);
}
public sealed class ProspectActivityRepository(SalesDbContext db) : EfRepository<ProspectActivity>(db), IProspectActivityRepository
{
    public async Task<IReadOnlyList<ProspectActivity>> ListByCandidateAsync(Guid id, CancellationToken ct = default) => await Db.ProspectActivities.AsNoTracking().Where(x => x.ProspectCandidateId == id).OrderBy(x => x.OccurredAt).ToArrayAsync(ct);
    public override Task<ProspectActivity> UpdateAsync(ProspectActivity entity, long expectedVersion, CancellationToken ct = default) => throw new NotSupportedException("Prospect activity is append-only.");
}
public sealed class ProspectConversionRepository(SalesDbContext db) : EfRepository<ProspectConversion>(db), IProspectConversionRepository
{
    public Task<ProspectConversion?> GetByCandidateAsync(Guid id, CancellationToken ct = default) => Db.ProspectConversions.AsNoTracking().SingleOrDefaultAsync(x => x.ProspectCandidateId == id, ct);
}
public sealed class ResearchTargetProfileRepository(SalesDbContext db) : EfRepository<ResearchTargetProfile>(db), IResearchTargetProfileRepository;
public sealed class ResearchRunRepository(SalesDbContext db) : EfRepository<ResearchRun>(db), IResearchRunRepository;
