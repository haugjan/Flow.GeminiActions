using System.Windows;
using Flow.GeminiActions.Editor;
using Flow.GeminiActions.GeminiClient;
using Flow.GeminiActions.Settings;
using Flow.Launcher.Plugin;

namespace Flow.GeminiActions.Actions;

internal interface IResultCreator
{
    Result CreateActionResult(GeminiAction action, string text);
    Result CreateOpenEditorResult(string text);
    Result CreateHint(string title, string subtitle);
    Result CreateError(string title, string subtitle);
}

internal sealed class ResultCreator(
    IGeminiClient gemini,
    PluginInitContext context,
    PluginSettings settings
) : IResultCreator
{
    private const string MainIcon = "Images/icon.png";
    private const string HintIcon = "Images/hint.png";

    public Result CreateActionResult(GeminiAction action, string text) =>
        new()
        {
            Title = action.Title,
            SubTitle = string.IsNullOrWhiteSpace(action.Description)
                ? "Run on the current text and copy result to clipboard."
                : action.Description,
            IcoPath = MainIcon,
            Action = ctx =>
            {
                _ = Task.Run(() => RunAsync(action, text));
                return true;
            },
        };

    public Result CreateOpenEditorResult(string text) =>
        new()
        {
            Title = "Open editor ...",
            SubTitle = "Edit text in a window. Pick an action and Ctrl+Enter to send.",
            IcoPath = MainIcon,
            Action = ctx =>
            {
                ShowEditor(text);
                return true;
            },
        };

    public Result CreateHint(string title, string subtitle) =>
        new()
        {
            Title = title,
            SubTitle = subtitle,
            IcoPath = HintIcon,
            Action = _ => false,
        };

    public Result CreateError(string title, string subtitle) =>
        new()
        {
            Title = title,
            SubTitle = subtitle,
            IcoPath = HintIcon,
            Action = _ => false,
        };

    private void ShowEditor(string text)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null)
            return;

        dispatcher.Invoke(() =>
        {
            var window = new EditorWindow(gemini, settings, context, text);
            window.Show();
            window.Activate();
        });
    }

    private async Task RunAsync(GeminiAction action, string text)
    {
        try
        {
            using var cts = new CancellationTokenSource(
                TimeSpan.FromSeconds(Math.Max(5, settings.Timeout.TotalSeconds))
            );
            var output = await gemini
                .GenerateAsync(action.Instruction, text, cts.Token)
                .ConfigureAwait(false);

            SetClipboard(output);
            context.API.ShowMsg(
                $"Gemini · {action.Title}",
                "Result copied to clipboard.",
                MainIcon
            );
        }
        catch (OperationCanceledException)
        {
            context.API.ShowMsg(
                $"Gemini · {action.Title}",
                "Request timed out. Increase the timeout in settings.",
                MainIcon
            );
        }
        catch (Exception ex)
        {
            context.API.LogException(nameof(ResultCreator), "Gemini call failed", ex);
            context.API.ShowMsg($"Gemini · {action.Title} failed", ex.Message, MainIcon);
        }
    }

    private static void SetClipboard(string text)
    {
        if (Application.Current?.Dispatcher is { } dispatcher)
            dispatcher.Invoke(() => Clipboard.SetText(text));
        else
            Clipboard.SetText(text);
    }
}
