using System.Security.Claims;
using SalesManagement.Core.Models;
using SalesManagement.Core.Rates;
using SalesManagement.Core.Repositories;
using SalesManagement.Core.Targets;

namespace SalesManagement.Api.Endpoints;

public static class RateTargetEndpoints
{
    public static IEndpointRouteBuilder MapRateTargetEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var rates = endpoints.MapGroup("/api/rates").RequireAuthorization();
        rates.MapGet("/monthly", (int year, int month, RateService service, CancellationToken ct) => service.ListMonthlyAsync(new YearMonth(year, month), ct));
        rates.MapPut("/monthly", async (MonthlyRateRequest request, RateService service, IUserRepository users, HttpContext context, CancellationToken ct) =>
            Results.Ok(await service.SaveMonthlyAsync(new YearMonth(request.Year, request.Month), request.Currency, request.Rate, request.ExpectedVersion, await Actor(context, users, ct), ct)));
        rates.MapGet("/daily", (DateOnly from, DateOnly to, RateService service, CancellationToken ct) => service.ListDailyAsync(from, to, ct));

        var targets = endpoints.MapGroup("/api/targets").RequireAuthorization();
        targets.MapGet("/", (int year, int month, TargetService service, CancellationToken ct) => service.ListAsync(new YearMonth(year, month), ct));
        targets.MapPut("/", async (TargetRequest request, TargetService service, IUserRepository users, HttpContext context, CancellationToken ct) =>
            Results.Ok(await service.SaveAsync(new YearMonth(request.Year, request.Month), request.UserId, request.TargetKrw, request.ExpectedVersion, await Actor(context, users, ct), ct)));
        return endpoints;
    }

    private static async Task<User> Actor(HttpContext context, IUserRepository users, CancellationToken ct)
    {
        var id = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(id, out var userId) || await users.GetAsync(userId, ct) is not { } user) throw new UnauthorizedAccessException();
        return user;
    }

    public sealed record MonthlyRateRequest(int Year, int Month, Currency Currency, decimal Rate, long ExpectedVersion);
    public sealed record TargetRequest(int Year, int Month, Guid UserId, decimal TargetKrw, long ExpectedVersion);
}
