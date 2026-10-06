using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using SalesManagement.Api.Endpoints;
using SalesManagement.Core.Network;
using SalesManagement.Core.Repositories;
using SalesManagement.InMemory.Repositories;
using SalesManagement.Core.Exceptions;

namespace SalesManagement.Tests;

public sealed class NetworkContractTests
{
    [Fact]
    public void Network_Endpoints_Match_Frontend_Routes_And_Methods()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddAuthorization();
        builder.Services.AddScoped<NetworkCompanyService>();
        builder.Services.AddScoped<NetworkRelationshipService>();
        builder.Services.AddScoped<NetworkTreeService>();
        builder.Services.AddScoped<CompanyContactService>();
        builder.Services.AddScoped<CompanyAssignmentService>();
        builder.Services.AddSingleton<IUserRepository, InMemoryUserRepository>();
        var app = builder.Build();
        app.MapNetworkEndpoints();

        var routes = ((IEndpointRouteBuilder)app).DataSources.SelectMany(x => x.Endpoints)
            .OfType<RouteEndpoint>()
            .Select(x => (Pattern: x.RoutePattern.RawText!, Methods: x.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods))
            .ToArray();

        AssertRoute(routes, "/api/network/companies", "GET", "POST");
        AssertRoute(routes, "/api/network/companies/{id:guid}", "GET", "PUT");
        AssertRoute(routes, "/api/network/companies/{id:guid}/disable", "POST");
        AssertRoute(routes, "/api/network/companies/{id:guid}/convert", "POST");
        AssertRoute(routes, "/api/network/tree", "GET");
        AssertRoute(routes, "/api/network/relationships", "GET", "POST");
        AssertRoute(routes, "/api/network/relationships/{id:guid}", "PUT");
        AssertRoute(routes, "/api/network/relationships/{id:guid}/disable", "POST");
        AssertRoute(routes, "/api/network/companies/{companyId:guid}/contacts", "GET", "POST");
        AssertRoute(routes, "/api/network/companies/{companyId:guid}/contacts/{contactId:guid}", "PUT");
        AssertRoute(routes, "/api/network/companies/{companyId:guid}/assignments", "GET", "POST");
    }

    [Fact]
    public void Mutating_Requests_Expose_ExpectedVersion()
    {
        Assert.Contains(nameof(NetworkEndpoints.UpdateNetworkCompanyRequest.ExpectedVersion), typeof(NetworkEndpoints.UpdateNetworkCompanyRequest).GetProperties().Select(x => x.Name));
        Assert.Contains(nameof(NetworkEndpoints.UpdateRelationshipRequest.ExpectedVersion), typeof(NetworkEndpoints.UpdateRelationshipRequest).GetProperties().Select(x => x.Name));
        Assert.Contains(nameof(NetworkEndpoints.UpdateContactRequest.ExpectedVersion), typeof(NetworkEndpoints.UpdateContactRequest).GetProperties().Select(x => x.Name));
        Assert.Contains(nameof(NetworkEndpoints.AssignmentChangeRequest.ExpectedVersion), typeof(NetworkEndpoints.AssignmentChangeRequest).GetProperties().Select(x => x.Name));
        Assert.Contains(nameof(NetworkEndpoints.ProspectConversionRequest.ExpectedVersion), typeof(NetworkEndpoints.ProspectConversionRequest).GetProperties().Select(x => x.Name));
    }

    [Theory]
    [InlineData(typeof(ForbiddenException), 403)]
    [InlineData(typeof(KeyNotFoundException), 404)]
    [InlineData(typeof(ConcurrencyException), 409)]
    [InlineData(typeof(NetworkRelationshipConflictException), 409)]
    [InlineData(typeof(NetworkValidationException), 422)]
    public void Network_Errors_Map_To_Approved_Statuses(Type exceptionType, int expected)
    {
        Exception exception = exceptionType == typeof(ConcurrencyException)
            ? new ConcurrencyException(Guid.NewGuid(), 1, 2)
            : (Exception)Activator.CreateInstance(exceptionType, "test")!;
        Assert.Equal(expected, NetworkProblemMapper.Map(exception).Status);
    }

    private static void AssertRoute((string Pattern, IReadOnlyList<string> Methods)[] routes, string pattern, params string[] methods)
    {
        var actual = routes.Where(x => x.Pattern == pattern).SelectMany(x => x.Methods).Distinct().ToArray();
        Assert.Equal(methods.Order(), actual.Order());
    }
}
