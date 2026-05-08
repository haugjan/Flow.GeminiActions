using System.Windows.Controls;

namespace Flow.GeminiActions.Settings;

internal interface IConfigurator
{
    Control CreateSettingPanel();
}

internal sealed class Configurator(SettingsViewModel viewModel) : IConfigurator
{
    public Control CreateSettingPanel() => new SettingsView { DataContext = viewModel };
}
