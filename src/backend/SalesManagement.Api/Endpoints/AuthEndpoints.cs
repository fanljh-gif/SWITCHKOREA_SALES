using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using SalesManagement.Core.Auth;
using SalesManagement.Core.Exceptions;
using SalesManagement.Core.Repositories;

namespace SalesManagement.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/auth");
        group.MapPost("/login", async (LoginRequest request, AuthService auth, HttpContext context, CancellationToken ct) =>
        {
            try
            {
                var result = await auth.LoginAsync(request.LoginId, request.Password, ct);
                var claims = new[] { new Claim(ClaimTypes.NameIdentifier, result.User.Id.ToString()), new Claim(ClaimTypes.Name, result.User.Name), new Claim(ClaimTypes.Role, result.User.Role.ToString()) };
                await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));
                return Results.Ok(new { user = result.User, passwordChangeRequired = result.PasswordChangeRequired });
            }
            catch (AccountLockedException exception) { return Results.Problem(exception.Message, statusCode: StatusCodes.Status423Locked); }
            catch (AuthenticationFailedException exception) { return Results.Problem(exception.Message, statusCode: StatusCodes.Status401Unauthorized); }
        });
        group.MapPost("/logout", async (HttpContext context) => { await context.SignOutAsync(); return Results.NoContent(); }).RequireAuthorization();
        group.MapGet("/me", async (HttpContext context, IUserRepository users, CancellationToken ct) =>
        {
            var id = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(id, out var userId) && await users.GetAsync(userId, ct) is { } user ? Results.Ok(user) : Results.Unauthorized();
        }).RequireAuthorization();
        group.MapPost("/change-password", async (ChangePasswordRequest request, HttpContext context, IUserRepository users, AuthService auth, CancellationToken ct) =>
        {
            var id = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(id, out var userId) || await users.GetAsync(userId, ct) is not { } user) return Results.Unauthorized();
            await auth.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword, ct);
            return Results.NoContent();
        }).RequireAuthorization();
        return endpoints;
    }

    public sealed record LoginRequest(string LoginId, string Password);
    public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
}
