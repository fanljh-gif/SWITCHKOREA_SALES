using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using SalesManagement.Core.Auth;

namespace SalesManagement.Api.Configuration;

public static class CookieAuthenticationConfiguration
{
    public static IServiceCollection AddSalesCookieAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
        {
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.SlidingExpiration = true;
            options.ExpireTimeSpan = AuthOptions.Default.SessionIdleTimeout;
            options.Events.OnRedirectToLogin = context => ApplyApiStatusCode(
                context,
                StatusCodes.Status401Unauthorized);
            options.Events.OnRedirectToAccessDenied = context => ApplyApiStatusCode(
                context,
                StatusCodes.Status403Forbidden);
        });
        return services;
    }

    private static Task ApplyApiStatusCode(
        RedirectContext<CookieAuthenticationOptions> context,
        int statusCode)
    {
        if (context.Request.Path.StartsWithSegments("/api"))
            context.Response.StatusCode = statusCode;
        else
            context.Response.Redirect(context.RedirectUri);

        return Task.CompletedTask;
    }
}
