using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using SalesManagement.Api.Endpoints;
using SalesManagement.Core.Repositories;
using SalesManagement.Core.Research;

namespace SalesManagement.Tests;

public sealed class ResearchContractTests
{
    [Fact]
    public void Research_Endpoints_Match_The_Frontend_Contract()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddAuthorization();
        builder.Services.AddScoped<IResearchTargetProfileRepository>(_ => null!);
        builder.Services.AddScoped<IResearchRunRepository>(_ => null!);
        builder.Services.AddScoped<IProspectCandidateRepository>(_ => null!);
        builder.Services.AddScoped<IResearchEvidenceRepository>(_ => null!);
        builder.Services.AddScoped<IDuplicateMatchRepository>(_ => null!);
        builder.Services.AddScoped<IProspectAssessmentRepository>(_ => null!);
        builder.Services.AddScoped<IUserRepository>(_ => null!);
        builder.Services.AddScoped<ResearchRunService>(_ => null!);
        builder.Services.AddScoped<ProspectReviewService>(_ => null!);
        builder.Services.AddScoped<ProspectExcelService>(_ => null!);
        var app = builder.Build();
        app.MapResearchEndpoints();
        var routes = ((IEndpointRouteBuilder)app).DataSources.SelectMany(x => x.Endpoints).OfType<RouteEndpoint>()
            .Select(x => (x.RoutePattern.RawText!, x.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods)).ToArray();

        AssertRoute(routes, "/api/research/summary", "GET");
        AssertRoute(routes, "/api/research/profiles", "PUT");
        AssertRoute(routes, "/api/research/runs", "POST");
        AssertRoute(routes, "/api/research/candidates", "GET");
        AssertRoute(routes, "/api/research/candidates/{id:guid}", "GET");
        AssertRoute(routes, "/api/research/candidates/{id:guid}/assign", "POST");
        AssertRoute(routes, "/api/research/candidates/{id:guid}/review", "POST");
        AssertRoute(routes, "/api/research/candidates/{id:guid}/approve", "POST");
        AssertRoute(routes, "/api/research/candidates/{id:guid}/duplicates", "GET");
        AssertRoute(routes, "/api/research/imports/validate", "POST");
        AssertRoute(routes, "/api/research/imports/{id:guid}/apply", "POST");
    }

    [Fact]
    public void Mutating_Requests_Require_ExpectedVersion()
    {
        Assert.NotNull(typeof(ResearchEndpoints.SaveProfileRequest).GetProperty("ExpectedVersion"));
        Assert.NotNull(typeof(ResearchEndpoints.AssignRequest).GetProperty("ExpectedVersion"));
        Assert.NotNull(typeof(ResearchEndpoints.ReviewRequest).GetProperty("ExpectedVersion"));
        Assert.NotNull(typeof(ResearchEndpoints.ApproveRequest).GetProperty("ExpectedVersion"));
    }

    private static void AssertRoute((string Pattern, IReadOnlyList<string> Methods)[] routes, string pattern, params string[] methods) =>
        Assert.Equal(methods.Order(), routes.Where(x => x.Pattern == pattern).SelectMany(x => x.Methods).Distinct().Order());
}
