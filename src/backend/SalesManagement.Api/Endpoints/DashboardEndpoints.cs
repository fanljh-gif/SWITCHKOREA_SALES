using SalesManagement.Core.Dashboard;

namespace SalesManagement.Api.Endpoints;

public static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/dashboard", (DateOnly from, DateOnly to, Guid? ownerId, Guid? customerId, Guid? productId, DashboardService service, CancellationToken ct) =>
            service.QueryAsync(new DashboardQuery(from, to, ownerId, customerId, productId), ct)).RequireAuthorization();
        return endpoints;
    }
}
