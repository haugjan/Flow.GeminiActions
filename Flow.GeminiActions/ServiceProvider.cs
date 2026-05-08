using System.Net.Http;
using Flow.GeminiActions.Actions;
using Flow.GeminiActions.GeminiClient;
using Flow.GeminiActions.Settings;
using Flow.Launcher.Plugin;
using Microsoft.Extensions.DependencyInjection;

namespace Flow.GeminiActions;

internal static class ServiceProvider
{
    public static void ConfigureServices(
        this ServiceCollection services,
        PluginInitContext context,
        PluginSettings settings
    )
    {
        services.AddSingleton(context);
        services.AddSingleton(settings);
        services.AddSingleton<Func<HttpClient>>(_ =>
            () =>
            {
                var http = new HttpClient
                {
                    BaseAddress = new Uri("https://generativelanguage.googleapis.com/"),
                    Timeout = TimeSpan.FromSeconds(
                        Math.Clamp(settings.Timeout.TotalSeconds, 5, 120)
                    ),
                };
                http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
                if (!string.IsNullOrWhiteSpace(settings.ApiKey))
                    http.DefaultRequestHeaders.Add("x-goog-api-key", settings.ApiKey);
                return http;
            }
        );

        services.AddScoped<IGeminiClient, GeminiClient.GeminiClient>();
        services.AddScoped<IResultCreator, ResultCreator>();
        services.AddScoped<IActionRunner, ActionRunner>();
        services.AddScoped<IConfigurator, Configurator>();
        services.AddScoped<SettingsViewModel>();
    }
}
