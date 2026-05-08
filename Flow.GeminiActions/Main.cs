using System.Windows.Controls;
using Flow.GeminiActions.Actions;
using Flow.GeminiActions.Settings;
using Flow.Launcher.Plugin;
using Microsoft.Extensions.DependencyInjection;

namespace Flow.GeminiActions;

public class Main : IAsyncPlugin, ISettingProvider
{
    private IActionRunner _runner = null!;
    private IConfigurator _configurator = null!;
    private SettingsViewModel _settingsViewModel = null!;

    public Task InitAsync(PluginInitContext context)
    {
        var services = new ServiceCollection();
        var settings = context.API.LoadSettingJsonStorage<PluginSettings>();
        if (settings.Actions.Count == 0)
            settings.Actions = PluginSettings.DefaultActions();

        services.ConfigureServices(context, settings);
        var provider = services.BuildServiceProvider();

        _runner = provider.GetRequiredService<IActionRunner>();
        _configurator = provider.GetRequiredService<IConfigurator>();
        _settingsViewModel = provider.GetRequiredService<SettingsViewModel>();
        return Task.CompletedTask;
    }

    public Task<List<Result>> QueryAsync(Query query, CancellationToken token)
    {
        _settingsViewModel.SyncActionsToSettings();
        return _runner.QueryAsync(
            query.Search ?? string.Empty,
            query.ActionKeyword ?? string.Empty,
            token
        );
    }

    public Control CreateSettingPanel() => _configurator.CreateSettingPanel();
}
