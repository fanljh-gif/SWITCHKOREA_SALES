using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SalesManagement.Core.Repositories;
using SalesManagement.Postgres.Repositories;
namespace SalesManagement.Postgres;
public static class PostgresServiceExtensions
{
    public static IServiceCollection AddSalesPostgres(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<SalesDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IUserRepository, UserRepository>(); services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IProductRepository, ProductRepository>(); services.AddScoped<IPriceRepository, PriceRepository>();
        services.AddScoped<IShipmentRepository, ShipmentRepository>(); services.AddScoped<IRateRepository, RateRepository>();
        services.AddScoped<ITargetRepository, TargetRepository>(); services.AddScoped<IImportBatchRepository, ImportBatchRepository>();
        services.AddScoped<IAuditRepository, AuditRepository>(); services.AddScoped<ITransactionRunner, PostgresTransactionRunner>();
        services.AddScoped<INetworkCompanyRepository, NetworkCompanyRepository>();
        services.AddScoped<ICompanyRelationshipRepository, CompanyRelationshipRepository>();
        services.AddScoped<ICompanyContactRepository, CompanyContactRepository>();
        services.AddScoped<ICompanyAssignmentRepository, CompanyAssignmentRepository>();
        services.AddScoped<IProspectCandidateRepository, ProspectCandidateRepository>();
        services.AddScoped<IResearchEvidenceRepository, ResearchEvidenceRepository>();
        services.AddScoped<IProspectAssessmentRepository, ProspectAssessmentRepository>();
        services.AddScoped<IDuplicateMatchRepository, DuplicateMatchRepository>();
        services.AddScoped<IProspectActivityRepository, ProspectActivityRepository>();
        services.AddScoped<IProspectConversionRepository, ProspectConversionRepository>();
        services.AddScoped<IResearchTargetProfileRepository, ResearchTargetProfileRepository>();
        services.AddScoped<IResearchRunRepository, ResearchRunRepository>();
        services.AddScoped<IInquiryRepository, InquiryRepository>();
        services.AddScoped<IQuoteRepository, QuoteRepository>();
        services.AddScoped<IIncotermRepository, IncotermRepository>();
        return services;
    }
}
