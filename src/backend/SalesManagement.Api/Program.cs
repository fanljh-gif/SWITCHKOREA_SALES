using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using System.Text.Json;
using System.Text.Json.Serialization;
using SalesManagement.Core.Admin;
using SalesManagement.Core.Audit;
using SalesManagement.Api.Endpoints;
using SalesManagement.Api.Configuration;
using SalesManagement.Core.Auth;
using SalesManagement.Core.Customers;
using SalesManagement.Core.Dashboard;
using SalesManagement.Core.Exceptions;
using SalesManagement.Core.Imports;
using SalesManagement.Core.Models;
using SalesManagement.Core.Network;
using SalesManagement.Core.Prices;
using SalesManagement.Core.Products;
using SalesManagement.Core.Rates;
using SalesManagement.Core.Research;
using SalesManagement.Api.Research;
using SalesManagement.Api.Workers;
using SalesManagement.Core.Repositories;
using SalesManagement.Core.Shipments;
using SalesManagement.Core.Targets;
using SalesManagement.Core.Quotations;
using SalesManagement.Api.Quotations;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));

builder.WebHost.UseIISIntegration();
builder.Services.AddSalesProxyRuntime(builder.Configuration, builder.Environment);
builder.Services.AddSalesProductionRuntime(builder.Configuration, builder.Environment);
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton(AuthOptions.Default);
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<PriceService>();
builder.Services.AddSingleton<ImportValidationStore>();
builder.Services.AddScoped<ShipmentService>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<AdminUserService>();
builder.Services.AddScoped<ExcelImportService>();
builder.Services.AddScoped<RateService>();
builder.Services.AddScoped<TargetService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<NetworkCompanyService>();
builder.Services.AddScoped<NetworkRelationshipService>();
builder.Services.AddScoped<NetworkTreeService>();
builder.Services.AddScoped<CompanyContactService>();
builder.Services.AddScoped<CompanyAssignmentService>();
builder.Services.AddScoped<DuplicateDetectionService>();
builder.Services.AddScoped<ProspectReviewService>();
builder.Services.AddScoped<ResearchRunService>();
builder.Services.AddScoped<ProspectExcelService>();
builder.Services.AddScoped<InquiryService>();
builder.Services.AddScoped<QuoteService>();
builder.Services.AddScoped<QuotationDashboardService>();
builder.Services.AddSingleton<IQuotePdfRenderer, SimpleQuotePdfRenderer>();
builder.Services.Configure<ResearchProviderOptions>(builder.Configuration.GetSection(ResearchProviderOptions.SectionName));
builder.Services.AddHttpClient<OpenAiProspectResearchProvider>(client => client.Timeout = TimeSpan.FromSeconds(45));
builder.Services.AddScoped<IProspectResearchProvider>(provider =>
    provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ResearchProviderOptions>>().Value.Provider.Equals("OpenAI", StringComparison.OrdinalIgnoreCase)
        ? provider.GetRequiredService<OpenAiProspectResearchProvider>()
        : new DisabledProspectResearchProvider());
builder.Services.AddHostedService<ResearchWorker>();
builder.Services.AddHostedService<WeeklyResearchWorker>();
builder.Services.AddSalesCookieAuthentication();
builder.Services.AddAuthorization();

var app = builder.Build();
app.UseSalesProxyRuntime(builder.Environment);
app.UseSalesHttpsPolicy(builder.Environment);
app.UseDefaultFiles();
app.UseStaticFiles();
app.Use(async (context, next) =>
{
    try { await next(); }
    catch (PricePeriodConflictException exception) { context.Response.StatusCode = StatusCodes.Status409Conflict; await context.Response.WriteAsJsonAsync(new { title = exception.Message, status = 409 }); }
    catch (InvalidImportBatchException exception) { context.Response.StatusCode = StatusCodes.Status422UnprocessableEntity; await context.Response.WriteAsJsonAsync(new { title = exception.Message, status = 422 }); }
    catch (Exception exception) when (NetworkProblemMapper.CanMap(exception))
    {
        var problem = NetworkProblemMapper.Map(exception);
        context.Response.StatusCode = problem.Status;
        await context.Response.WriteAsJsonAsync(new { title = problem.Title, status = problem.Status, detail = problem.Detail });
    }
});
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready"),
});
app.MapAuthEndpoints();
app.MapMasterDataEndpoints();
app.MapShipmentEndpoints();
app.MapRateTargetEndpoints();
app.MapDashboardEndpoints();
app.MapImportEndpoints();
app.MapAdminEndpoints();
app.MapNetworkEndpoints();
app.MapResearchEndpoints();
app.MapInquiryEndpoints();
app.MapQuoteEndpoints();
app.MapIncotermEndpoints();
app.MapFallbackToFile("index.html");
app.Run();

public partial class Program;
