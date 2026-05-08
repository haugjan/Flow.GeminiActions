using System.Windows;
using System.Windows.Input;
using Flow.GeminiActions.GeminiClient;
using Flow.GeminiActions.Settings;
using Flow.Launcher.Plugin;

namespace Flow.GeminiActions.Editor;

public partial class EditorWindow : Window
{
    private readonly IGeminiClient _gemini;
    private readonly PluginSettings _settings;
    private readonly PluginInitContext _context;
    private CancellationTokenSource? _cts;

    internal EditorWindow(
        IGeminiClient gemini,
        PluginSettings settings,
        PluginInitContext context,
        string initialText
    )
    {
        InitializeComponent();
        _gemini = gemini;
        _settings = settings;
        _context = context;

        ActionCombo.ItemsSource = settings.Actions;
        ActionCombo.SelectedIndex = 0;

        InputBox.Text = initialText;
        InputBox.Focus();
        InputBox.SelectionStart = InputBox.Text.Length;
    }

    private void InputBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control)
        {
            e.Handled = true;
            _ = SendAsync(autoCopy: true);
        }
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            Close();
            return;
        }
        base.OnPreviewKeyDown(e);
    }

    private async void Send_Click(object sender, RoutedEventArgs e) =>
        await SendAsync(autoCopy: false);

    private void Copy_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(OutputBox.Text))
        {
            Clipboard.SetText(OutputBox.Text);
            StatusText.Text = "Result copied to clipboard.";
        }
    }

    private async Task SendAsync(bool autoCopy)
    {
        if (ActionCombo.SelectedItem is not GeminiAction action)
        {
            StatusText.Text = "No action selected.";
            return;
        }

        var text = InputBox.Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            StatusText.Text = "Input is empty.";
            return;
        }

        _cts?.Cancel();
        _cts = new CancellationTokenSource(
            TimeSpan.FromSeconds(Math.Max(5, _settings.Timeout.TotalSeconds))
        );

        SendButton.IsEnabled = false;
        Spinner.Visibility = Visibility.Visible;
        StatusText.Text = $"Running '{action.Title}' ...";
        OutputBox.Text = string.Empty;

        try
        {
            var output = await _gemini
                .GenerateAsync(action.Instruction, text, _cts.Token)
                .ConfigureAwait(true);
            OutputBox.Text = output;
            if (autoCopy)
            {
                Clipboard.SetText(output);
                StatusText.Text = $"Done · {output.Length} chars · copied to clipboard.";
            }
            else
            {
                StatusText.Text = $"Done · {output.Length} chars · Ctrl+Enter to re-run.";
            }
        }
        catch (OperationCanceledException)
        {
            StatusText.Text = "Cancelled or timed out.";
        }
        catch (Exception ex)
        {
            _context.API.LogException(nameof(EditorWindow), "Gemini call failed", ex);
            StatusText.Text = $"Error: {ex.Message}";
        }
        finally
        {
            Spinner.Visibility = Visibility.Collapsed;
            SendButton.IsEnabled = true;
        }
    }
}
