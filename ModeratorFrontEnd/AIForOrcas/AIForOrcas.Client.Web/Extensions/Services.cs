using Microsoft.AspNetCore.Components.Server.Circuits;

namespace AIForOrcas.Client.Web.Extensions;

public static class Services
{
    public static void ConfigureDataServices(this WebApplicationBuilder builder)
    {
        // Register server-side token store as singleton
        builder.Services.AddSingleton<ITokenStore, ServerSideTokenStore>();
        
        // Register circuit handler
        builder.Services.AddScoped<CircuitHandlerService>();
        builder.Services.AddScoped<CircuitHandler>(sp => sp.GetRequiredService<CircuitHandlerService>());

        // Register authentication provider.
        builder.Services.AddScoped<ApiAuthenticationStateProvider>();
        builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
            sp.GetRequiredService<ApiAuthenticationStateProvider>());

        // Register handlers as SCOPED (not Transient) to match their dependencies
        builder.Services.AddScoped<LoggingHandler>();
        builder.Services.AddScoped<AuthenticationHeaderHandler>();

        // Register HTTP clients with handlers.
        builder.Services.AddHttpClient("UnauthenticatedAPI", (sp, client) =>
        {
            var apiUrl = sp.GetRequiredService<AppSettings>().APIUrl;
            client.BaseAddress = new Uri(apiUrl);
        })
        .AddHttpMessageHandler<LoggingHandler>();

        builder.Services.AddHttpClient("AuthenticatedAPI", (sp, client) =>
        {
            var apiUrl = sp.GetRequiredService<AppSettings>().APIUrl;
            client.BaseAddress = new Uri(apiUrl);
        })
        .AddHttpMessageHandler<AuthenticationHeaderHandler>()
        .AddHttpMessageHandler<LoggingHandler>();

        // Remove the old factory registration and use this instead:
        builder.Services.AddScoped<IDetectionService, DetectionService>();

        builder.Services.AddScoped<IMetricsService, MetricsService>();
        builder.Services.AddScoped<ITagService, TagService>();
        builder.Services.AddScoped<IAccountService, AccountService>();
    }

    public static void ConfigureWebServices(this WebApplicationBuilder builder, AppSettings appSettings)
    {
        // Removed - no longer needed
    }
}
