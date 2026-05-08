using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Flow.GeminiActions.Settings;

public partial class SettingsView
{
    public SettingsView()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            if (DataContext is SettingsViewModel vm)
                ApiKeyBox.Password = vm.Settings.ApiKey;
        };
    }

    private void ApiKeyBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm)
            vm.Settings.ApiKey = ((PasswordBox)sender).Password;
    }

    private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !int.TryParse(e.Text, out _);
    }
}
