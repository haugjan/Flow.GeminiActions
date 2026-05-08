using System.Windows;
using Flow.GeminiActions.GeminiClient;
using Flow.GeminiActions.Settings;
using Flow.Launcher.Plugin;

namespace Flow.GeminiActions.Actions;

internal interface IResultCreator
{
    Result CreateActionResult(GeminiAction action, string text);
    Result CreateHint(string title, string subtitle);
    Result CreateError(string title, string subtitle);
}

internal sealed class ResultCreator(
    IGeminiClient gemini,
    PluginInitContext context,
    PluginSettings settings
) : IResultCreator
{
    public Result CreateActionResult(GeminiAction action, string text) =>
        new()
        {
            Title = action.Title,
            SubTitle = string.IsNullOrWhiteSpace(action.Description)
                ? Preview(text)
                : $"{action.Description}  ·  {Preview(text)}",
            IcoPath = "Images/icon.png",
            Action = ctx =>
            {
                _ = Task.Run(() => RunAsync(action, text));
                return true;
            },
        };

    public Result CreateHint(string title, string subtitle) =>
        new()
        {
            Title = title,
            SubTitle = subtitle,
            IcoPath = "Images/gray.png",
            Action = _ => false,
        };

    public Result CreateError(string title, string subtitle) =>
        new()
        {
            Title = title,
            SubTitle = subtitle,
            IcoPath = "Images/gray.png",
            Action = _ => false,
        };

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
                "Images/icon.png"
            );
        }
        catch (OperationCanceledException)
        {
            context.API.ShowMsg(
                $"Gemini · {action.Title}",
                "Request timed out. Increase the timeout in settings.",
                "Images/icon.png"
            );
        }
        catch (Exception ex)
        {
            context.API.LogException(nameof(ResultCreator), "Gemini call failed", ex);
            context.API.ShowMsg($"Gemini · {action.Title} failed", ex.Message, "Images/icon.png");
        }
    }

    private static void SetClipboard(string text)
    {
        if (Application.Current?.Dispatcher is { } dispatcher)
            dispatcher.Invoke(() => Clipboard.SetText(text));
        else
            Clipboard.SetText(text);
    }

    private static string Preview(string text)
    {
        var oneLine = text.Replace('\n', ' ').Replace('\r', ' ').Trim();
        return oneLine.Length <= 80 ? oneLine : oneLine[..77] + "...";
    }
}
