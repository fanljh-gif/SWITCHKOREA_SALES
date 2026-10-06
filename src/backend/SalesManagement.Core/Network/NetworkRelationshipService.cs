using SalesManagement.Core.Exceptions;
using SalesManagement.Core.Models;
using SalesManagement.Core.Repositories;

namespace SalesManagement.Core.Network;

public sealed record RelationshipSupplyCommand(Guid ProductId, string ApplicationName, string? Notes);
public sealed record NetworkRelationshipSaveCommand(Guid? Id, Guid UpstreamCompanyId, Guid DownstreamCompanyId,
    DateOnly ValidFrom, DateOnly? ValidTo, string? Notes, EntityStatus Status, IReadOnlyList<RelationshipSupplyCommand> Supplies);

public sealed class NetworkRelationshipService(ICompanyRelationshipRepository relationships, INetworkCompanyRepository companies,
    ICompanyAssignmentRepository assignments, IProductRepository? products = null, IAuditRepository? audits = null)
{
    public async Task<IReadOnlyList<CompanyRelationship>> ListAsync(Guid? companyId, CancellationToken ct = default) =>
        companyId.HasValue ? await relationships.ListByCompanyAsync(companyId.Value, ct) : await relationships.ListAsync(ct);

    public async Task<CompanyRelationship> SaveRelationshipAsync(NetworkRelationshipSaveCommand command, User actor,
        long expectedVersion = 0, CancellationToken ct = default)
    {
        SalesManagement.Core.Auth.AuthorizationService.EnsureActive(actor);
        if (command.UpstreamCompanyId == command.DownstreamCompanyId) throw new NetworkCycleException("동일 회사를 상·하위로 연결할 수 없습니다.");
        if (command.ValidTo < command.ValidFrom) throw new NetworkValidationException("종료일은 시작일보다 빠를 수 없습니다.");
        if (command.Supplies.Count == 0) throw new NetworkValidationException("납품 품목을 한 개 이상 등록해야 합니다.");
        var upstream = await companies.GetAsync(command.UpstreamCompanyId, ct) ?? throw new KeyNotFoundException("상위 거래선을 찾을 수 없습니다.");
        var downstream = await companies.GetAsync(command.DownstreamCompanyId, ct) ?? throw new KeyNotFoundException("하위 거래선을 찾을 수 없습니다.");
        await EnsureCanEditEither(actor, upstream, downstream, ct);
        foreach (var supply in command.Supplies)
        {
            if (string.IsNullOrWhiteSpace(supply.ApplicationName)) throw new NetworkValidationException("적용 제품 또는 프로젝트는 필수입니다.");
            if (products is not null && await products.GetAsync(supply.ProductId, ct) is null) throw new KeyNotFoundException("납품 품목을 찾을 수 없습니다.");
        }
        var all = await relationships.ListAsync(ct);
        if (all.Any(x => x.Id != command.Id && x.Status == EntityStatus.Active && x.UpstreamCompanyId == command.UpstreamCompanyId && x.DownstreamCompanyId == command.DownstreamCompanyId && Overlaps(x.ValidFrom, x.ValidTo, command.ValidFrom, command.ValidTo)))
            throw new NetworkRelationshipConflictException();
        if (command.Status == EntityStatus.Active && Reaches(command.DownstreamCompanyId, command.UpstreamCompanyId, command.Id, command.ValidFrom, command.ValidTo, all, ct)) throw new NetworkCycleException();

        if (command.Id is null)
        {
            var relationship = new CompanyRelationship { UpstreamCompanyId = command.UpstreamCompanyId, DownstreamCompanyId = command.DownstreamCompanyId, ValidFrom = command.ValidFrom, ValidTo = command.ValidTo, Notes = command.Notes, Status = command.Status };
            relationship.Supplies = command.Supplies.Select(x => new RelationshipSupply { RelationshipId = relationship.Id, ProductId = x.ProductId, ApplicationName = x.ApplicationName, Notes = x.Notes }).ToList();
            var saved = await relationships.AddAsync(relationship, ct);
            await RecordAudit(actor, saved, "Created", ct);
            return saved;
        }
        var existing = await relationships.GetAsync(command.Id.Value, ct) ?? throw new KeyNotFoundException("관계를 찾을 수 없습니다.");
        var existingUp = await companies.GetAsync(existing.UpstreamCompanyId, ct) ?? throw new KeyNotFoundException("기존 상위 거래선을 찾을 수 없습니다.");
        var existingDown = await companies.GetAsync(existing.DownstreamCompanyId, ct) ?? throw new KeyNotFoundException("기존 하위 거래선을 찾을 수 없습니다.");
        await EnsureCanEditEither(actor, existingUp, existingDown, ct);
        if (existing.Version != expectedVersion) throw new ConcurrencyException(existing.Id, expectedVersion, existing.Version);
        existing.UpstreamCompanyId = command.UpstreamCompanyId; existing.DownstreamCompanyId = command.DownstreamCompanyId; existing.ValidFrom = command.ValidFrom; existing.ValidTo = command.ValidTo; existing.Notes = command.Notes; existing.Status = command.Status;
        existing.Supplies = command.Supplies.Select(x => new RelationshipSupply { RelationshipId = existing.Id, ProductId = x.ProductId, ApplicationName = x.ApplicationName, Notes = x.Notes }).ToList();
        var updated = await relationships.UpdateAsync(existing, expectedVersion, ct);
        await RecordAudit(actor, updated, "Updated", ct);
        return updated;
    }

    public async Task DisableAsync(Guid id, long expectedVersion, User actor, CancellationToken ct = default)
    {
        SalesManagement.Core.Auth.AuthorizationService.EnsureActive(actor);
        var relationship = await relationships.GetAsync(id, ct) ?? throw new KeyNotFoundException("관계를 찾을 수 없습니다.");
        var up = await companies.GetAsync(relationship.UpstreamCompanyId, ct) ?? throw new KeyNotFoundException();
        var down = await companies.GetAsync(relationship.DownstreamCompanyId, ct) ?? throw new KeyNotFoundException();
        await EnsureCanEditEither(actor, up, down, ct);
        if (relationship.Version != expectedVersion) throw new ConcurrencyException(relationship.Id, expectedVersion, relationship.Version);
        relationship.Status = EntityStatus.Inactive;
        await relationships.UpdateAsync(relationship, expectedVersion, ct);
        await RecordAudit(actor, relationship, "Disabled", ct);
    }

    private async Task EnsureCanEditEither(User actor, NetworkCompany up, NetworkCompany down, CancellationToken ct)
    {
        if (actor.Role == Role.Manager) return;
        var upAssignment = (await assignments.ListByCompanyAsync(up.Id, ct)).SingleOrDefault(x => x.IsCurrent);
        var downAssignment = (await assignments.ListByCompanyAsync(down.Id, ct)).SingleOrDefault(x => x.IsCurrent);
        if ((upAssignment?.UserId ?? up.CurrentOwnerUserId) != actor.Id && (downAssignment?.UserId ?? down.CurrentOwnerUserId) != actor.Id)
            throw new ForbiddenException();
    }

    private static bool Overlaps(DateOnly aFrom, DateOnly? aTo, DateOnly bFrom, DateOnly? bTo) => aFrom <= (bTo ?? DateOnly.MaxValue) && bFrom <= (aTo ?? DateOnly.MaxValue);
    private static bool Reaches(Guid start, Guid target, Guid? ignoredId, DateOnly validFrom, DateOnly? validTo, IReadOnlyList<CompanyRelationship> all, CancellationToken ct)
    {
        var pending = new Stack<Guid>(); var visited = new HashSet<Guid>(); pending.Push(start);
        while (pending.TryPop(out var id) && visited.Add(id))
        {
            ct.ThrowIfCancellationRequested();
            if (id == target) return true;
            foreach (var edge in all.Where(x => x.Id != ignoredId && x.Status == EntityStatus.Active && x.UpstreamCompanyId == id && Overlaps(x.ValidFrom, x.ValidTo, validFrom, validTo))) pending.Push(edge.DownstreamCompanyId);
        }
        return false;
    }

    private async Task RecordAudit(User actor, CompanyRelationship relationship, string action, CancellationToken ct)
    {
        if (audits is null) return;
        var json = $"{{\"UpstreamCompanyId\":\"{relationship.UpstreamCompanyId}\",\"DownstreamCompanyId\":\"{relationship.DownstreamCompanyId}\"}}";
        await audits.AddAsync(new AuditEntry { ActorUserId = actor.Id, Target = relationship.UpstreamCompanyId.ToString(), Action = action, AfterJson = json }, ct);
        await audits.AddAsync(new AuditEntry { ActorUserId = actor.Id, Target = relationship.DownstreamCompanyId.ToString(), Action = action, AfterJson = json }, ct);
    }
}
