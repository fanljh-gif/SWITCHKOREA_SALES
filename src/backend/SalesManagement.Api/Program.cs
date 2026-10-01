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

var app = builder.Build();
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
app.Run();

public partial class Program;
