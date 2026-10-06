using Microsoft.EntityFrameworkCore;
using SalesManagement.Core.Exceptions;
using SalesManagement.Core.Models;
using SalesManagement.Core.Repositories;

namespace SalesManagement.Postgres.Repositories;

public sealed class CompanyAssignmentRepository(SalesDbContext db) : EfRepository<CompanyAssignment>(db), ICompanyAssignmentRepository
{
    public async Task<IReadOnlyList<CompanyAssignment>> ListByCompanyAsync(Guid companyId, CancellationToken ct = default) =>
        await Db.CompanyAssignments.AsNoTracking().Where(x => x.CompanyId == companyId).OrderBy(x => x.ValidFrom).ToArrayAsync(ct);

    public override async Task<CompanyAssignment> AddAsync(CompanyAssignment entity, CancellationToken ct = default)
    {
        await EnsureNoOverlap(entity, ct);
        return await base.AddAsync(entity, ct);
    }

    public override async Task<CompanyAssignment> UpdateAsync(CompanyAssignment entity, long expectedVersion, CancellationToken ct = default)
    {
        await EnsureNoOverlap(entity, ct);
        return await base.UpdateAsync(entity, expectedVersion, ct);
    }

    private async Task EnsureNoOverlap(CompanyAssignment candidate, CancellationToken ct)
    {
        var end = candidate.ValidTo ?? DateOnly.MaxValue;
        var overlaps = await Db.CompanyAssignments.AsNoTracking().AnyAsync(x => x.CompanyId == candidate.CompanyId && x.Id != candidate.Id &&
            x.ValidFrom <= end && candidate.ValidFrom <= (x.ValidTo ?? DateOnly.MaxValue), ct);
        if (overlaps) throw new NetworkValidationException("담당자 배정 기간이 기존 이력과 겹칩니다.");
    }
}
