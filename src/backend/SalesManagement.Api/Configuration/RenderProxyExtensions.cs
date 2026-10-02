using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HostFiltering;

namespace SalesManagement.Api.Configuration;

public static class RenderProxyExtensions
{
    public static IServiceCollection AddSalesProxyRuntime(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        if (!environment.IsProduction()
            || ProductionRuntimeOptions.ParseHostingMode(configuration) != ProductionHostingMode.Render)
            return services;

        var hostname = configuration[ProductionRuntimeOptions.RenderExternalHostnameVariable];
        if (string.IsNullOrWhiteSpace(hostname))
            throw new InvalidOperationException(
                $"{ProductionRuntimeOptions.RenderExternalHostnameVariable} is required in Render mode.");

        services.Configure<HostFilteringOptions>(options =>
        {
            options.AllowedHosts.Clear();
            options.AllowedHosts.Add(hostname);
            options.AllowedHosts.Add("localhost");
            options.AllowedHosts.Add("127.0.0.1");
        });
        return services;
    }

    public static WebApplication UseSalesProxyRuntime(
        this WebApplication app,
        IWebHostEnvironment environment)
    {
        if (!environment.IsProduction()
            || ProductionRuntimeOptions.ParseHostingMode(app.Configuration) != ProductionHostingMode.Render)
            return app;

        app.UseHostFiltering();
        return app;
    }

    public static WebApplication UseSalesHttpsPolicy(
        this WebApplication app,
        IWebHostEnvironment environment)
    {
        if (!environment.IsProduction())
            return app;

        app.UseHsts();
        if (ProductionRuntimeOptions.ParseHostingMode(app.Configuration) != ProductionHostingMode.Render)
            app.UseHttpsRedirection();
        return app;
    }
}
