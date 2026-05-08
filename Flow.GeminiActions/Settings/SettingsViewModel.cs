using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Flow.GeminiActions.Settings;

public sealed class SettingsViewModel : INotifyPropertyChanged
{
    private readonly PluginSettings _settings;

    public SettingsViewModel(PluginSettings settings)
    {
        _settings = settings;
        Actions = new ObservableCollection<GeminiAction>(settings.Actions);
        Actions.CollectionChanged += (_, _) => SyncActionsToSettings();

        AddActionCommand = new RelayCommand(_ =>
        {
            Actions.Add(
                new GeminiAction
                {
                    Title = "New action",
                    Description = string.Empty,
                    Instruction = string.Empty,
                }
            );
        });

        RemoveActionCommand = new RelayCommand(arg =>
        {
            if (arg is GeminiAction action)
                Actions.Remove(action);
        });

        ResetDefaultsCommand = new RelayCommand(_ =>
        {
            Actions.Clear();
            foreach (var a in PluginSettings.DefaultActions())
                Actions.Add(a);
        });
    }

    public PluginSettings Settings => _settings;

    public ObservableCollection<GeminiAction> Actions { get; }

    public string TimeoutSeconds
    {
        get => ((int)_settings.Timeout.TotalSeconds).ToString();
        set
        {
            if (int.TryParse(value, out var seconds) && seconds > 0)
            {
                _settings.Timeout = TimeSpan.FromSeconds(seconds);
                OnPropertyChanged();
            }
        }
    }

    public ICommand AddActionCommand { get; }
    public ICommand RemoveActionCommand { get; }
    public ICommand ResetDefaultsCommand { get; }

    public void SyncActionsToSettings()
    {
        _settings.Actions = Actions.ToList();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

internal sealed class RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
    : ICommand
{
    public bool CanExecute(object? parameter) => canExecute?.Invoke(parameter) ?? true;

    public void Execute(object? parameter) => execute(parameter);

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
}
