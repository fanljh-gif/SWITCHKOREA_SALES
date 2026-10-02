using Microsoft.AspNetCore.Authentication.Cookies;
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
using SalesManagement.Core.Prices;
using SalesManagement.Core.Products;
using SalesManagement.Core.Rates;
using SalesManagement.Core.Repositories;
using SalesManagement.Core.Shipments;
using SalesManagement.Core.Targets;

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
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = AuthOptions.Default.SessionIdleTimeout;
});
builder.Services.AddAuthorization();

var app = builder.Build();
app.UseSalesProxyRuntime(builder.Environment);
app.UseSalesHttpsPolicy(builder.Environment);
app.UseDefaultFiles();
app.UseStaticFiles();
app.Use(async (context, next) =>
{
    try { await next(); }
    catch (ForbiddenException exception) { context.Response.StatusCode = StatusCodes.Status403Forbidden; await context.Response.WriteAsJsonAsync(new { title = exception.Message, status = 403 }); }
    catch (PricePeriodConflictException exception) { context.Response.StatusCode = StatusCodes.Status409Conflict; await context.Response.WriteAsJsonAsync(new { title = exception.Message, status = 409 }); }
    catch (InvalidImportBatchException exception) { context.Response.StatusCode = StatusCodes.Status422UnprocessableEntity; await context.Response.WriteAsJsonAsync(new { title = exception.Message, status = 422 }); }
    catch (ConcurrencyException exception) { context.Response.StatusCode = StatusCodes.Status409Conflict; await context.Response.WriteAsJsonAsync(new { title = "다른 사용자가 먼저 수정했습니다.", status = 409, detail = exception.Message }); }
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
app.MapFallbackToFile("index.html");
app.Run();

public partial class Program;
