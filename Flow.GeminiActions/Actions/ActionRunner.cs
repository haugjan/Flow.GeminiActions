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

        // If the typed text is a prefix of an action title the user is filtering
        // the list, not providing input. Fall back to clipboard in that case and
        // narrow the visible actions to those whose title starts with the typed text.
        var validActions = settings.Actions.Where(a =>
            !string.IsNullOrWhiteSpace(a.Title) && !string.IsNullOrWhiteSpace(a.Instruction)
        ).ToList();

        var isActionFilter = hasTyped && validActions.Any(a =>
            a.Title.StartsWith(typed, StringComparison.OrdinalIgnoreCase)
        );

        // With no text (or when the text is an action filter) the source is the clipboard.
        // Read it once here only to decide whether to show action rows or the
        // empty-state hints; the text actually sent to Gemini is read again
        // when the result fires (see textProvider below). Flow Launcher caches
        // result rows, so a clipboard value captured at query time could be
        // replayed long after the user copied something else.
        var clipboard = (hasTyped && !isActionFilter) ? string.Empty : ReadClipboard();
        var fromClipboard = isActionFilter
            ? !string.IsNullOrEmpty(clipboard)
            : !hasTyped && !string.IsNullOrEmpty(clipboard);

        // Show empty hints only when there is no typed input AND no action filter active.
        // If the user is filtering by action name, show the matching actions even when
        // the clipboard is empty — they may still add text by typing after the filter word.
        var hasInput = (hasTyped && !isActionFilter) || fromClipboard;
        if (!hasInput && !isActionFilter)
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
        Func<string> textProvider = (hasTyped && !isActionFilter) ? () => typed : ReadClipboard;

        var filteredActions = isActionFilter
            ? validActions.Where(a => a.Title.StartsWith(typed, StringComparison.OrdinalIgnoreCase))
            : validActions;

        var results = filteredActions
            .Select(a => resultCreator.CreateActionResult(a, textProvider))
            .ToList();

        if (hasTyped && !isActionFilter)
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
        else if (isActionFilter)
            results.Insert(
                0,
                resultCreator.CreateHint(
                    "Clipboard is empty",
                    "Copy text to the clipboard first, then trigger the action."
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
