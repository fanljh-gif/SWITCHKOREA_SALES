using System.Security.Claims;
using SalesManagement.Core.Admin;
using SalesManagement.Core.Audit;
using SalesManagement.Core.Models;
using SalesManagement.Core.Repositories;

namespace SalesManagement.Api.Endpoints;

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/admin").RequireAuthorization();
        group.MapGet("/users", async (AdminUserService service, CancellationToken ct) => (await service.ListAsync(ct)).Select(PublicUser));
        group.MapPost("/users", async (CreateUserCommand command, AdminUserService service, IUserRepository users, HttpContext context, CancellationToken ct) => Results.Ok(PublicUser(await service.CreateAsync(command, await Actor(context, users, ct), ct))));
        group.MapPut("/users/{id:guid}/status", async (Guid id, StatusRequest request, AdminUserService service, IUserRepository users, HttpContext context, CancellationToken ct) => Results.Ok(PublicUser(await service.SetStatusAsync(id, request.Status, request.ExpectedVersion, await Actor(context, users, ct), ct))));
        group.MapPut("/users/{id:guid}/role", async (Guid id, RoleRequest request, AdminUserService service, IUserRepository users, HttpContext context, CancellationToken ct) => Results.Ok(PublicUser(await service.SetRoleAsync(id, request.Role, request.ExpectedVersion, await Actor(context, users, ct), ct))));
        group.MapGet("/audit", (string target, AuditService service, CancellationToken ct) => service.ListAsync(target, ct));
        return endpoints;
    }

    private static object PublicUser(User user) => new { user.Id, user.LoginId, user.Name, user.Email, user.Role, user.Status, user.PasswordChangeRequired, user.Version };
    private static async Task<User> Actor(HttpContext context, IUserRepository users, CancellationToken ct)
    {
        var id = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(id, out var userId) || await users.GetAsync(userId, ct) is not { } user) throw new UnauthorizedAccessException();
        return user;
    }
    public sealed record StatusRequest(EntityStatus Status, long ExpectedVersion);
    public sealed record RoleRequest(Role Role, long ExpectedVersion);
}
