using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using SalesManagement.Api.Endpoints;
using SalesManagement.Core.Auth;
using SalesManagement.Core.Customers;
using SalesManagement.Core.Exceptions;
using SalesManagement.Core.Models;
using SalesManagement.Core.Prices;
using SalesManagement.Core.Products;
using SalesManagement.Core.Repositories;
using SalesManagement.InMemory.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IUserRepository, InMemoryUserRepository>();
builder.Services.AddSingleton<ICustomerRepository, InMemoryCustomerRepository>();
builder.Services.AddSingleton<IProductRepository, InMemoryProductRepository>();
builder.Services.AddSingleton<IPriceRepository, InMemoryPriceRepository>();
builder.Services.AddSingleton<IShipmentRepository, InMemoryShipmentRepository>();
builder.Services.AddSingleton<IRateRepository, InMemoryRateRepository>();
builder.Services.AddSingleton<ITargetRepository, InMemoryTargetRepository>();
builder.Services.AddSingleton<IAuditRepository, InMemoryAuditRepository>();
builder.Services.AddSingleton<IImportBatchRepository, InMemoryImportBatchRepository>();
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton(AuthOptions.Default);
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<PriceService>();
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
app.Use(async (context, next) =>
{
    try { await next(); }
    catch (ForbiddenException exception) { context.Response.StatusCode = StatusCodes.Status403Forbidden; await context.Response.WriteAsJsonAsync(new { title = exception.Message, status = 403 }); }
    catch (PricePeriodConflictException exception) { context.Response.StatusCode = StatusCodes.Status409Conflict; await context.Response.WriteAsJsonAsync(new { title = exception.Message, status = 409 }); }
    catch (ConcurrencyException exception) { context.Response.StatusCode = StatusCodes.Status409Conflict; await context.Response.WriteAsJsonAsync(new { title = "다른 사용자가 먼저 수정했습니다.", status = 409, detail = exception.Message }); }
});
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
app.MapAuthEndpoints();
app.MapMasterDataEndpoints();
app.Run();

public partial class Program;
