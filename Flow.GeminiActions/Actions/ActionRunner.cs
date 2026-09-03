using System.Windows;
using Flow.GeminiActions.Settings;
using Flow.Launcher.Plugin;

namespace Flow.GeminiActions.Actions;

internal interface IActionRunner
{
    Task<List<Result>> QueryAsync(string searchText, string actionKeyword, CancellationToken token);
}

internal sealed class ActionRunner(PluginSettings settings, IResultCreator resultCreator)
    : IActionRunner
{
    public Task<List<Result>> QueryAsync(
        string searchText,
        string actionKeyword,
        CancellationToken token
    )
    {
        var typed = searchText?.Trim() ?? string.Empty;
        var hasTyped = !string.IsNullOrEmpty(typed);

        // With no text after the action keyword the source is the clipboard.
        // Read it once here only to decide whether to show action rows or the
        // empty-state hints; the text actually sent to Gemini is read again
        // when the result fires (see textProvider below). Flow Launcher caches
        // result rows, so a clipboard value captured at query time could be
        // replayed long after the user copied something else.
        var clipboard = hasTyped ? string.Empty : ReadClipboard();
        var fromClipboard = !hasTyped && !string.IsNullOrEmpty(clipboard);

        if (!hasTyped && !fromClipboard)
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

        // Typed text is fixed for this query. For the clipboard fallback the
        // provider re-reads the clipboard the moment the user picks an action,
        // so it always reflects what is on the clipboard "now".
        Func<string> textProvider = hasTyped ? () => typed : ReadClipboard;

        var results = settings
            .Actions.Where(a =>
                !string.IsNullOrWhiteSpace(a.Title) && !string.IsNullOrWhiteSpace(a.Instruction)
            )
            .Select(a => resultCreator.CreateActionResult(a, textProvider))
            .ToList();

        if (hasTyped)
            results.Insert(
                0,
                resultCreator.CreateHint(
                    $"Using typed text ({typed.Length} chars)",
                    $"Clear the input after \"{actionKeyword}\" to use the clipboard instead."
                )
            );
        else if (fromClipboard)
            results.Insert(
                0,
                resultCreator.CreateHint(
                    "Using clipboard text",
                    $"{clipboard.Length} characters on the clipboard. Type after \"{actionKeyword}\" to override."
                )
            );

        results.Add(resultCreator.CreateOpenEditorResult(textProvider));

        return Task.FromResult(results);
    }

    private List<Result> EmptyHints()
    {
        var hints = new List<Result>
        {
            resultCreator.CreateHint(
                "Type some text after the action keyword",
                "Or copy text to the clipboard first, then trigger the plugin without arguments."
            ),
            resultCreator.CreateHint(
                $"{settings.Actions.Count} actions configured",
                "Translate, Correct, Bullets to text ... edit them in plugin settings."
            ),
            resultCreator.CreateOpenEditorResult(() => string.Empty),
        };
        return hints;
    }

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
}
