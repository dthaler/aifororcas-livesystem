namespace AIForOrcas.Client.Web.Extensions;

public static class Services
{
    public static void ConfigureDataServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddTransient<IDetectionService, DetectionService>();
        builder.Services.AddTransient<IMetricsService, MetricsService>();
        builder.Services.AddTransient<ITagService, TagService>();
    }

    public static void ConfigureWebServices(this WebApplicationBuilder builder, AppSettings appSettings)
    {
        if (!string.IsNullOrWhiteSpace(appSettings?.APIUrl))
        {
            // Register the authentication handler.
            builder.Services.AddScoped<AuthenticationHeaderHandler>();

            // Register HttpClient with the handler.
            builder.Services.AddScoped(sp =>
            {
                var handler = sp.GetRequiredService<AuthenticationHeaderHandler>();
                handler.InnerHandler = new HttpClientHandler();

                var client = new HttpClient(handler, disposeHandler: false);
                client.BaseAddress = new System.Uri(appSettings.APIUrl);
                return client;
            });
        }
    }
}
