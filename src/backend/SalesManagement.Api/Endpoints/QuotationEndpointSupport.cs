using System.Security.Claims; using SalesManagement.Core.Models; using SalesManagement.Core.Repositories;
namespace SalesManagement.Api.Endpoints;
internal static class QuotationEndpointSupport { public static async Task<User> Actor(HttpContext context, IUserRepository users, CancellationToken ct) { var value = context.User.FindFirstValue(ClaimTypes.NameIdentifier); if (!Guid.TryParse(value, out var id) || await users.GetAsync(id, ct) is not { } user) throw new UnauthorizedAccessException(); return user; } }
