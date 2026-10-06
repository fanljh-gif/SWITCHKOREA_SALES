using System.Security.Claims;
using SalesManagement.Core.Models;
using SalesManagement.Core.Network;
using SalesManagement.Core.Repositories;
using SalesManagement.Core.Exceptions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace SalesManagement.Api.Endpoints;

public static class NetworkEndpoints
{
    public static IEndpointRouteBuilder MapNetworkEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/network").RequireAuthorization();

        group.MapGet("/companies", async (Guid? rootCompanyId, string? search, string? lifecycleType, Guid? ownerId, string? status,
            NetworkCompanyService service, NetworkTreeService treeService, CancellationToken ct) =>
        {
            var rows = await service.ListAsync(ct);
            HashSet<Guid>? rootIds = null;
            if (rootCompanyId.HasValue)
            {
                rootIds = Flatten(await treeService.BuildTreeAsync(new NetworkTreeQuery(RootCompanyId: rootCompanyId), ct));
            }
            return Results.Ok(rows.Where(x =>
                (rootIds is null || rootIds.Contains(x.Id)) &&
                (string.IsNullOrWhiteSpace(search) || $"{x.CompanyName} {x.Country} {x.Industry}".Contains(search, StringComparison.OrdinalIgnoreCase)) &&
                (!ownerId.HasValue || x.CurrentOwnerUserId == ownerId) &&
                (string.IsNullOrWhiteSpace(status) || x.Status == ParseStatus(status)) &&
                MatchesLifecycle(x, lifecycleType))
                .Select(ToResponse));
        });
        group.MapGet("/companies/{id:guid}", async (Guid id, NetworkCompanyService service, CancellationToken ct) =>
            await service.GetAsync(id, ct) is { } company ? Results.Ok(ToResponse(company)) : Results.NotFound());
        group.MapPost("/companies", async (NetworkCompanyRequest request, NetworkCompanyService service, IUserRepository users, HttpContext context, CancellationToken ct) =>
        {
            var actor = await Actor(context, users, ct);
            var command = request.ToCommand(null, request.OwnerId ?? actor.Id);
            return Results.Ok(ToResponse(await service.SaveCompanyAsync(command, actor, 0, ct)));
        });
        group.MapPut("/companies/{id:guid}", async (Guid id, UpdateNetworkCompanyRequest request, NetworkCompanyService service, IUserRepository users, HttpContext context, CancellationToken ct) =>
        {
            var actor = await Actor(context, users, ct);
            var ownerId = request.OwnerId ?? (await service.GetAsync(id, ct))?.CurrentOwnerUserId ?? actor.Id;
            return Results.Ok(ToResponse(await service.SaveCompanyAsync(request.ToCommand(id, ownerId), actor, request.ExpectedVersion, ct)));
        });
        group.MapPost("/companies/{id:guid}/disable", async (Guid id, ExpectedVersionRequest request, NetworkCompanyService service, IUserRepository users, HttpContext context, CancellationToken ct) =>
        { await service.DisableAsync(id, request.ExpectedVersion, await Actor(context, users, ct), ct); return Results.NoContent(); });
        group.MapPost("/companies/{id:guid}/convert", async (Guid id, ProspectConversionRequest request, NetworkCompanyService service, IUserRepository users, HttpContext context, CancellationToken ct) =>
            Results.Ok(ToResponse(await service.ConvertProspectAsync(id, request.CustomerCode, request.DefaultCurrency, request.ExpectedVersion, await Actor(context, users, ct), ct))));

        group.MapGet("/tree", async (Guid? rootCompanyId, string? lifecycleType, Guid? ownerId, string? status, string? search,
            NetworkTreeService service, CancellationToken ct) =>
        {
            var query = new NetworkTreeQuery(rootCompanyId,
                string.IsNullOrWhiteSpace(lifecycleType) || lifecycleType.Equals("oem", StringComparison.OrdinalIgnoreCase) ? null : ParseLifecycle(lifecycleType),
                ownerId, string.IsNullOrWhiteSpace(status) ? null : ParseStatus(status), search,
                lifecycleType?.Equals("oem", StringComparison.OrdinalIgnoreCase) == true);
            return Results.Ok((await service.BuildTreeAsync(query, ct)).Select(ToResponse));
        });

        group.MapGet("/relationships", async (Guid? companyId, NetworkRelationshipService service, CancellationToken ct) =>
            Results.Ok((await service.ListAsync(companyId, ct)).Select(ToResponse)));
        group.MapPost("/relationships", async (RelationshipRequest request, NetworkRelationshipService service, IUserRepository users, HttpContext context, CancellationToken ct) =>
            Results.Ok(ToResponse(await service.SaveRelationshipAsync(request.ToCommand(null), await Actor(context, users, ct), 0, ct))));
        group.MapPut("/relationships/{id:guid}", async (Guid id, UpdateRelationshipRequest request, NetworkRelationshipService service, IUserRepository users, HttpContext context, CancellationToken ct) =>
            Results.Ok(ToResponse(await service.SaveRelationshipAsync(request.ToCommand(id), await Actor(context, users, ct), request.ExpectedVersion, ct))));
        group.MapPost("/relationships/{id:guid}/disable", async (Guid id, ExpectedVersionRequest request, NetworkRelationshipService service, IUserRepository users, HttpContext context, CancellationToken ct) =>
        { await service.DisableAsync(id, request.ExpectedVersion, await Actor(context, users, ct), ct); return Results.NoContent(); });

        group.MapGet("/companies/{companyId:guid}/contacts", async (Guid companyId, CompanyContactService service, CancellationToken ct) =>
            Results.Ok(await service.ListAsync(companyId, ct)));
        group.MapPost("/companies/{companyId:guid}/contacts", async (Guid companyId, ContactRequest request, CompanyContactService service, IUserRepository users, HttpContext context, CancellationToken ct) =>
            Results.Ok(await service.SaveContactAsync(companyId, request.ToCommand(null), await Actor(context, users, ct), 0, ct)));
        group.MapPut("/companies/{companyId:guid}/contacts/{contactId:guid}", async (Guid companyId, Guid contactId, UpdateContactRequest request, CompanyContactService service, IUserRepository users, HttpContext context, CancellationToken ct) =>
            Results.Ok(await service.SaveContactAsync(companyId, request.ToCommand(contactId), await Actor(context, users, ct), request.ExpectedVersion, ct)));

        group.MapGet("/companies/{companyId:guid}/assignments", async (Guid companyId, CompanyAssignmentService service, CancellationToken ct) =>
            Results.Ok(await service.ListAsync(companyId, ct)));
        group.MapPost("/companies/{companyId:guid}/assignments", async (Guid companyId, AssignmentChangeRequest request, CompanyAssignmentService service, IUserRepository users, HttpContext context, CancellationToken ct) =>
            Results.Ok(await service.ChangeAssignmentAsync(companyId, request.UserId, request.EffectiveDate, request.ExpectedVersion, await Actor(context, users, ct), ct)));
        return endpoints;
    }

    private static async Task<User> Actor(HttpContext context, IUserRepository users, CancellationToken ct)
    {
        var id = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(id, out var userId) || await users.GetAsync(userId, ct) is not { } user) throw new UnauthorizedAccessException();
        return user;
    }

    private static LifecycleType ParseLifecycle(string value) => Enum.TryParse<LifecycleType>(value, true, out var result) ? result : throw new SalesManagement.Core.Exceptions.NetworkValidationException("지원하지 않는 고객 구분입니다.");
    private static EntityStatus ParseStatus(string value) => Enum.TryParse<EntityStatus>(value, true, out var result) ? result : throw new SalesManagement.Core.Exceptions.NetworkValidationException("지원하지 않는 상태입니다.");
    private static bool MatchesLifecycle(NetworkCompany company, string? value) => string.IsNullOrWhiteSpace(value) ||
        (value.Equals("oem", StringComparison.OrdinalIgnoreCase) ? company.IsOem : !company.IsOem && company.LifecycleType == ParseLifecycle(value));
    private static HashSet<Guid> Flatten(IReadOnlyList<NetworkTreeNode> roots)
    {
        var result = new HashSet<Guid>();
        var pending = new Stack<NetworkTreeNode>(roots.Reverse());
        while (pending.TryPop(out var node))
        {
            result.Add(node.Company.Id);
            foreach (var child in node.Children) pending.Push(child);
        }
        return result;
    }
    private static NetworkCompanyResponse ToResponse(NetworkCompany x) => new(x.Id, x.CompanyName, x.Country, x.Industry, x.LifecycleType, x.IsOem, x.CustomerId, x.CurrentOwnerUserId, x.Status, x.Version);
    private static RelationshipResponse ToResponse(CompanyRelationship x) => new(x.Id, x.UpstreamCompanyId, x.DownstreamCompanyId, x.ValidFrom, x.ValidTo, x.Notes, x.Status, x.Version, x.Supplies);
    private static NetworkTreeResponse ToResponse(NetworkTreeNode x) => new(ToResponse(x.Company), x.Depth, x.PathCompanyIds, x.Children.Select(ToResponse).ToArray());

    public record NetworkCompanyRequest(string CompanyName, string Country, string Industry, LifecycleType LifecycleType, bool IsOem, EntityStatus Status, Guid? OwnerId)
    { public NetworkCompanySaveCommand ToCommand(Guid? id, Guid ownerId) => new(id, CompanyName, Country, Industry, LifecycleType, IsOem, ownerId, Status); }
    public sealed record UpdateNetworkCompanyRequest(string CompanyName, string Country, string Industry, LifecycleType LifecycleType, bool IsOem, EntityStatus Status, Guid? OwnerId, long ExpectedVersion)
    { public NetworkCompanySaveCommand ToCommand(Guid id, Guid ownerId) => new(id, CompanyName, Country, Industry, LifecycleType, IsOem, ownerId, Status); }
    public sealed record ExpectedVersionRequest(long ExpectedVersion);
    public sealed record ProspectConversionRequest(string CustomerCode, Currency DefaultCurrency, long ExpectedVersion);
    public record RelationshipRequest(Guid UpstreamCompanyId, Guid DownstreamCompanyId, DateOnly ValidFrom, DateOnly? ValidTo, string? Notes, EntityStatus Status, IReadOnlyList<RelationshipSupplyCommand> Supplies)
    { public NetworkRelationshipSaveCommand ToCommand(Guid? id) => new(id, UpstreamCompanyId, DownstreamCompanyId, ValidFrom, ValidTo, Notes, Status, Supplies); }
    public sealed record UpdateRelationshipRequest(Guid UpstreamCompanyId, Guid DownstreamCompanyId, DateOnly ValidFrom, DateOnly? ValidTo, string? Notes, EntityStatus Status, IReadOnlyList<RelationshipSupplyCommand> Supplies, long ExpectedVersion)
    { public NetworkRelationshipSaveCommand ToCommand(Guid id) => new(id, UpstreamCompanyId, DownstreamCompanyId, ValidFrom, ValidTo, Notes, Status, Supplies); }
    public record ContactRequest(string Name, string Department, string Position, string BusinessRole, string MobilePhone, string Email, DateOnly ValidFrom, DateOnly? ValidTo, EmploymentStatus EmploymentStatus)
    { public CompanyContactSaveCommand ToCommand(Guid? id) => new(id, Name, Department, Position, BusinessRole, MobilePhone, Email, ValidFrom, ValidTo, EmploymentStatus); }
    public sealed record UpdateContactRequest(string Name, string Department, string Position, string BusinessRole, string MobilePhone, string Email, DateOnly ValidFrom, DateOnly? ValidTo, EmploymentStatus EmploymentStatus, long ExpectedVersion)
    { public CompanyContactSaveCommand ToCommand(Guid id) => new(id, Name, Department, Position, BusinessRole, MobilePhone, Email, ValidFrom, ValidTo, EmploymentStatus); }
    public sealed record AssignmentChangeRequest(Guid UserId, DateOnly EffectiveDate, long ExpectedVersion);
    public sealed record NetworkCompanyResponse(Guid Id, string CompanyName, string Country, string Industry, LifecycleType LifecycleType, bool IsOem, Guid? CustomerId, Guid CurrentOwnerId, EntityStatus Status, long Version);
    public sealed record RelationshipResponse(Guid Id, Guid UpstreamCompanyId, Guid DownstreamCompanyId, DateOnly ValidFrom, DateOnly? ValidTo, string? Notes, EntityStatus Status, long Version, IReadOnlyList<RelationshipSupply> Supplies);
    public sealed record NetworkTreeResponse(NetworkCompanyResponse Company, int Depth, IReadOnlyList<Guid> PathCompanyIds, IReadOnlyList<NetworkTreeResponse> Children);
}

public sealed record NetworkProblem(int Status, string Title, string? Detail = null);

public static class NetworkProblemMapper
{
    public static bool CanMap(Exception exception) => exception is UnauthorizedAccessException or ForbiddenException or KeyNotFoundException or ConcurrencyException or NetworkRelationshipConflictException or NetworkValidationException || IsConstraintViolation(exception);
    public static NetworkProblem Map(Exception exception) => exception switch
    {
        UnauthorizedAccessException => new(401, "로그인이 필요하거나 사용자 계정을 확인할 수 없습니다."),
        ForbiddenException => new(403, exception.Message),
        KeyNotFoundException => new(404, exception.Message),
        ConcurrencyException => new(409, "다른 사용자가 먼저 수정했습니다.", exception.Message),
        NetworkRelationshipConflictException => new(409, exception.Message),
        DbUpdateException dbException when IsConstraintViolation(dbException) => new(409, "다른 요청과 충돌했거나 중복된 데이터입니다. 새로고침 후 다시 시도해 주세요."),
        NetworkValidationException => new(422, exception.Message),
        _ => throw new ArgumentOutOfRangeException(nameof(exception)),
    };

    private static bool IsConstraintViolation(Exception exception) =>
        exception is DbUpdateException { InnerException: PostgresException postgres } && postgres.SqlState.StartsWith("23", StringComparison.Ordinal);
}
