using System.Security.Claims;
using SalesManagement.Core.Customers;
using SalesManagement.Core.Models;
using SalesManagement.Core.Prices;
using SalesManagement.Core.Products;
using SalesManagement.Core.Repositories;

namespace SalesManagement.Api.Endpoints;

public static class MasterDataEndpoints
{
    public static IEndpointRouteBuilder MapMasterDataEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/customers", (CustomerService service, CancellationToken ct) => service.ListAsync(ct)).RequireAuthorization();
        endpoints.MapPost("/api/customers", async (CustomerSaveRequest request, CustomerService service, IUserRepository users, HttpContext context, CancellationToken ct) =>
            Results.Ok(await service.SaveAsync(request.Command, await Actor(context, users, ct), request.ExpectedVersion, ct))).RequireAuthorization();
        endpoints.MapGet("/api/products", (ProductService service, CancellationToken ct) => service.ListAsync(ct)).RequireAuthorization();
        endpoints.MapPost("/api/products", async (ProductSaveRequest request, ProductService service, IUserRepository users, HttpContext context, CancellationToken ct) =>
            Results.Ok(await service.SaveAsync(request.Command, await Actor(context, users, ct), request.ExpectedVersion, ct))).RequireAuthorization();
        endpoints.MapGet("/api/prices", (PriceService service, CancellationToken ct) => service.ListAsync(ct)).RequireAuthorization();
        endpoints.MapPost("/api/prices", async (PriceSaveRequest request, PriceService service, IUserRepository users, HttpContext context, CancellationToken ct) =>
            Results.Ok(await service.SaveAsync(request.Command, await Actor(context, users, ct), request.ExpectedVersion, ct))).RequireAuthorization();
        return endpoints;
    }

    private static async Task<User> Actor(HttpContext context, IUserRepository users, CancellationToken ct)
    {
        var id = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(id, out var userId) || await users.GetAsync(userId, ct) is not { } user) throw new UnauthorizedAccessException();
        return user;
    }

    public sealed record CustomerSaveRequest(CustomerSaveCommand Command, long ExpectedVersion);
    public sealed record ProductSaveRequest(ProductSaveCommand Command, long ExpectedVersion);
    public sealed record PriceSaveRequest(PriceSaveCommand Command, long ExpectedVersion);
}
