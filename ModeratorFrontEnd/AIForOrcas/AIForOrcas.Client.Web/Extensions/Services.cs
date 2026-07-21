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

        // Register authentication provider
        builder.Services.AddScoped<ApiAuthenticationStateProvider>();
        builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
            sp.GetRequiredService<ApiAuthenticationStateProvider>());

        // Register handlers
        builder.Services.AddTransient<LoggingHandler>();
        builder.Services.AddTransient<AuthenticationHeaderHandler>();

        // Register HTTP clients with handlers
        builder.Services.AddHttpClient("UnauthenticatedAPI", client =>
        {
            client.BaseAddress = new Uri("https://aifororcasdetectionsstaging-fqecdpbbe2gkbma3.westus2-01.azurewebsites.net/");
        }).AddHttpMessageHandler<LoggingHandler>();

        builder.Services.AddHttpClient("AuthenticatedAPI", client =>
        {
            client.BaseAddress = new Uri("https://aifororcasdetectionsstaging-fqecdpbbe2gkbma3.westus2-01.azurewebsites.net/");
        })
            .AddHttpMessageHandler<AuthenticationHeaderHandler>()
            .AddHttpMessageHandler<LoggingHandler>();

        // Register DetectionService
        builder.Services.AddTransient<IDetectionService>(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            return new DetectionService(
                factory.CreateClient("UnauthenticatedAPI"),
                factory.CreateClient("AuthenticatedAPI"));
        });

        builder.Services.AddTransient<IMetricsService, MetricsService>();
        builder.Services.AddTransient<ITagService, TagService>();
        builder.Services.AddTransient<IAccountService, AccountService>();
    }

    public static void ConfigureWebServices(this WebApplicationBuilder builder, AppSettings appSettings)
    {
        // Remove HttpClient registration - no longer needed
    }
}
