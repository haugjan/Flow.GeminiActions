using System.Windows;
using Flow.GeminiActions.Editor;
using Flow.GeminiActions.GeminiClient;
using Flow.GeminiActions.Settings;
using Flow.Launcher.Plugin;

namespace Flow.GeminiActions.Actions;

internal interface IResultCreator
{
    Result CreateActionResult(GeminiAction action, Func<string> textProvider);
    Result CreateOpenEditorResult(Func<string> textProvider);
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

    public Result CreateActionResult(GeminiAction action, Func<string> textProvider) =>
        new()
        {
            Title = action.Title,
            SubTitle = string.IsNullOrWhiteSpace(action.Description)
                ? "Run on the current text and copy result to clipboard."
                : action.Description,
            IcoPath = MainIcon,
            Action = ctx =>
            {
                // Resolve the text now, on the Flow Launcher UI thread, before
                // the launcher window hides. For the clipboard fallback this
                // reads the live clipboard rather than a stale captured value.
                var text = textProvider();
                _ = Task.Run(() => RunAsync(action, text));
                return true;
            },
        };

    public Result CreateOpenEditorResult(Func<string> textProvider) =>
        new()
        {
            Title = "Open editor ...",
            SubTitle = "Edit text in a window. Pick an action and Ctrl+Enter to send.",
            IcoPath = MainIcon,
            Action = ctx =>
            {
                ShowEditor(textProvider());
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
        var overlay = ShowOverlay($"Gemini · {action.Title}");
        // Allow headroom on top of the user-configured timeout so the
        // overload retry window (5 s + 10 s + three attempts) doesn't tip
        // the whole operation into a hard timeout.
        var totalBudget = TimeSpan.FromSeconds(Math.Max(5, settings.Timeout.TotalSeconds) * 3 + 15);
        using var cts = new CancellationTokenSource(totalBudget);
        var userCancelled = false;
        using var escHook = new EscapeHook(() =>
        {
            userCancelled = true;
            cts.Cancel();
        });

        try
        {
            var output = await gemini
                .GenerateAsync(
                    action.Instruction,
                    text,
                    cts.Token,
                    onOverloaded: delay => CountdownAsync(overlay, action.Title, delay, cts.Token)
                )
                .ConfigureAwait(false);

            SetClipboard(output);
            FinishOverlay(overlay, "Result copied to clipboard.", success: true);
        }
        catch (OperationCanceledException)
        {
            if (userCancelled)
                CloseOverlay(overlay);
            else
                FinishOverlay(
                    overlay,
                    "Request timed out. Increase the timeout in settings.",
                    success: false
                );
        }
        catch (Exception ex)
        {
            context.API.LogException(nameof(ResultCreator), "Gemini call failed", ex);
            FinishOverlay(overlay, $"Failed: {ex.Message}", success: false);
        }
    }

    private static async Task CountdownAsync(
        SpinnerOverlay? overlay,
        string actionTitle,
        TimeSpan delay,
        CancellationToken token
    )
    {
        var seconds = (int)Math.Ceiling(delay.TotalSeconds);
        for (var s = seconds; s > 0; s--)
        {
            UpdateOverlayTitle(overlay, $"Gemini overloaded · retrying in {s}s ...");
            await Task.Delay(TimeSpan.FromSeconds(1), token).ConfigureAwait(false);
        }
        UpdateOverlayTitle(overlay, $"Gemini · {actionTitle} (retry)");
    }

    private static void UpdateOverlayTitle(SpinnerOverlay? overlay, string title)
    {
        if (overlay is null)
            return;
        Application.Current?.Dispatcher.Invoke(() => overlay.SetTitle(title));
    }

    private static SpinnerOverlay? ShowOverlay(string title)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null)
            return null;

        SpinnerOverlay? overlay = null;
        dispatcher.Invoke(() =>
        {
            overlay = new SpinnerOverlay(title);
            overlay.Show();
        });
        return overlay;
    }

    private static void FinishOverlay(SpinnerOverlay? overlay, string message, bool success)
    {
        if (overlay is null)
            return;
        Application.Current?.Dispatcher.Invoke(() => overlay.ShowResult(message, success));
    }

    private static void CloseOverlay(SpinnerOverlay? overlay)
    {
        if (overlay is null)
            return;
        Application.Current?.Dispatcher.Invoke(overlay.Close);
    }

    private static void SetClipboard(string text)
    {
        if (Application.Current?.Dispatcher is { } dispatcher)
            dispatcher.Invoke(() => Clipboard.SetText(text));
        else
            Clipboard.SetText(text);
    }
}
