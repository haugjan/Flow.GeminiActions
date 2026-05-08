using System.Windows;
using Flow.GeminiActions.Settings;
using Flow.Launcher.Plugin;

namespace Flow.GeminiActions.Actions;

internal interface IActionRunner
{
    Task<List<Result>> QueryAsync(Query query, CancellationToken token);
}

internal sealed class ActionRunner(PluginSettings settings, IResultCreator resultCreator)
    : IActionRunner
{
    public Task<List<Result>> QueryAsync(Query query, CancellationToken token)
    {
        var typed = query.Search?.Trim() ?? string.Empty;
        var text = string.IsNullOrEmpty(typed) ? ReadClipboard() : typed;
        var fromClipboard = string.IsNullOrEmpty(typed) && !string.IsNullOrEmpty(text);

        if (string.IsNullOrWhiteSpace(text))
            return Task.FromResult(EmptyHints());

        if (string.IsNullOrWhiteSpace(settings.ApiKey))
            return Task.FromResult(
                new List<Result>
                {
                    resultCreator.CreateError(
                        "No Gemini API key configured",
                        "Open Flow Launcher settings → Plugins → Gemini Actions and paste your API key."
                    ),
                }
            );

        if (settings.Actions.Count == 0)
            return Task.FromResult(
                new List<Result>
                {
                    resultCreator.CreateError(
                        "No actions defined",
                        "Add at least one action in the plugin settings."
                    ),
                }
            );

        var results = settings
            .Actions.Where(a =>
                !string.IsNullOrWhiteSpace(a.Title) && !string.IsNullOrWhiteSpace(a.Instruction)
            )
            .Select(a => resultCreator.CreateActionResult(a, text))
            .ToList();

        if (fromClipboard)
            results.Insert(
                0,
                resultCreator.CreateHint(
                    "Using clipboard text",
                    $"Type after \"{query.ActionKeyword}\" to override. Source: {Preview(text)}"
                )
            );

        return Task.FromResult(results);
    }

    private List<Result> EmptyHints() =>
        [
            resultCreator.CreateHint(
                "Type some text after the action keyword",
                "Or copy text to the clipboard first, then trigger the plugin without arguments."
            ),
            resultCreator.CreateHint(
                $"{settings.Actions.Count} actions configured",
                "Translate, Correct, Bullets to text ... edit them in plugin settings."
            ),
        ];

    private static string ReadClipboard()
    {
        try
        {
            string? text = null;
            if (Application.Current?.Dispatcher is { } dispatcher)
            {
                dispatcher.Invoke(() =>
                {
                    if (Clipboard.ContainsText())
                        text = Clipboard.GetText();
                });
            }
            else if (Clipboard.ContainsText())
            {
                text = Clipboard.GetText();
            }
            return text?.Trim() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string Preview(string text)
    {
        var oneLine = text.Replace('\n', ' ').Replace('\r', ' ').Trim();
        return oneLine.Length <= 60 ? oneLine : oneLine[..57] + "...";
    }
}
