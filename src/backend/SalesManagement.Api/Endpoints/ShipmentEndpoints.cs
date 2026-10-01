using System.Security.Claims;
using SalesManagement.Core.Models;
using SalesManagement.Core.Repositories;
using SalesManagement.Core.Shipments;

namespace SalesManagement.Api.Endpoints;

public static class ShipmentEndpoints
{
    public static IEndpointRouteBuilder MapShipmentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/shipments").RequireAuthorization();
        group.MapGet("/", (ShipmentService service, CancellationToken ct) => service.ListAsync(ct));
        group.MapPost("/", async (ShipmentCreateCommand command, ShipmentService service, IUserRepository users, HttpContext context, CancellationToken ct) => Results.Ok(await service.SaveAsync(command, await Actor(context, users, ct), ct)));
        group.MapPost("/{id:guid}/cancel", async (Guid id, VersionRequest request, ShipmentService service, IUserRepository users, HttpContext context, CancellationToken ct) => Results.Ok(await service.CancelAsync(id, await Actor(context, users, ct), request.ExpectedVersion, ct)));
        group.MapPost("/recalculate", async (RecalculateRequest request, ShipmentService service, IUserRepository users, HttpContext context, CancellationToken ct) => Results.Ok(await service.RecalculateAsync(request.ShipmentIds, await Actor(context, users, ct), request.ConfirmationToken, ct)));
        return endpoints;
    }

    private static async Task<User> Actor(HttpContext context, IUserRepository users, CancellationToken ct)
    {
        var id = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(id, out var userId) || await users.GetAsync(userId, ct) is not { } user) throw new UnauthorizedAccessException();
        return user;
    }

    public sealed record VersionRequest(long ExpectedVersion);
    public sealed record RecalculateRequest(Guid[] ShipmentIds, string? ConfirmationToken);
}
