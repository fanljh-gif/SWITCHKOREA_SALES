using SalesManagement.Core.Auth; using SalesManagement.Core.Models; using SalesManagement.Core.Repositories;
namespace SalesManagement.Api.Endpoints;
public static class IncotermEndpoints
{
    public static IEndpointRouteBuilder MapIncotermEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/admin/incoterms").RequireAuthorization();
        group.MapGet("", async (IIncotermRepository repo, CancellationToken ct) => Results.Ok((await repo.ListAsync(ct)).OrderBy(x => x.SortOrder)));
        group.MapPost("", async (SaveRequest r, IIncotermRepository repo, IUserRepository users, HttpContext c, CancellationToken ct) => { AuthorizationService.EnsureManager(await QuotationEndpointSupport.Actor(c, users, ct)); if (await repo.FindByCodeAsync(r.Code, ct) is not null) return Results.Conflict(new { title = "이미 등록된 Incoterms 코드입니다.", status = 409 }); return Results.Ok(await repo.AddAsync(new Incoterm { Code = r.Code.Trim().ToUpperInvariant(), DisplayName = r.DisplayName.Trim(), Status = r.Status, SortOrder = r.SortOrder }, ct)); });
        group.MapPut("/{id:guid}", async (Guid id, SaveRequest r, IIncotermRepository repo, IUserRepository users, HttpContext c, CancellationToken ct) => { AuthorizationService.EnsureManager(await QuotationEndpointSupport.Actor(c, users, ct)); var row = await repo.GetAsync(id, ct) ?? throw new KeyNotFoundException(); row.Code = r.Code.Trim().ToUpperInvariant(); row.DisplayName = r.DisplayName.Trim(); row.Status = r.Status; row.SortOrder = r.SortOrder; return Results.Ok(await repo.UpdateAsync(row, r.ExpectedVersion, ct)); });
        return endpoints;
    }
    public sealed record SaveRequest(string Code, string DisplayName, EntityStatus Status, int SortOrder, long ExpectedVersion = 0);
}
