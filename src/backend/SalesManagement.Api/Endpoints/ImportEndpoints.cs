using System.Security.Claims;
using SalesManagement.Core.Imports;
using SalesManagement.Core.Models;
using SalesManagement.Core.Repositories;

namespace SalesManagement.Api.Endpoints;

public static class ImportEndpoints
{
    public static IEndpointRouteBuilder MapImportEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/imports").RequireAuthorization();
        group.MapPost("/validate", async (IFormFile file, ImportKind kind, ExcelImportService service, IUserRepository users, HttpContext context, CancellationToken ct) =>
        {
            await using var stream = file.OpenReadStream();
            return Results.Ok(await service.ValidateAsync(stream, kind, file.FileName, await Actor(context, users, ct), ct));
        }).DisableAntiforgery();
        group.MapPost("/{batchId:guid}/commit", async (Guid batchId, ExcelImportService service, IUserRepository users, HttpContext context, CancellationToken ct) =>
            Results.Ok(await service.CommitAsync(batchId, await Actor(context, users, ct), ct)));
        return endpoints;
    }

    private static async Task<User> Actor(HttpContext context, IUserRepository users, CancellationToken ct)
    {
        var id = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(id, out var userId) || await users.GetAsync(userId, ct) is not { } user) throw new UnauthorizedAccessException();
        return user;
    }
}
