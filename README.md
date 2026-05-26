# Flow.GeminiActions

[![Build](https://github.com/haugjan/Flow.GeminiActions/actions/workflows/build-action.yml/badge.svg)](https://github.com/haugjan/Flow.GeminiActions/actions/workflows/build-action.yml)
[![Release](https://img.shields.io/github/v/release/haugjan/Flow.GeminiActions)](https://github.com/haugjan/Flow.GeminiActions/releases/latest)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

[Flow Launcher](https://www.flowlauncher.com/) plugin that runs your text
through pre-configured Gemini prompts (translate, correct, rewrite, …)
and copies the result to the clipboard.

Triggered with the `ask` action keyword.

## How it works

1. Type `ask <text>` in Flow Launcher — or copy text to the clipboard
   and trigger `ask` with no arguments.
2. Pick one of the configured actions: **Translate**, **Correct**,
   **Bullets to text**, or any custom shortcut you've added.
3. The plugin sends `<instruction>\n\n---\n<text>` to Gemini. While the
   request is in flight, a small dark pill appears in the centre of the
   working area showing a rotating spinner and the action name. When
   the response arrives the spinner becomes a green check mark,
   `Result copied to clipboard.` is shown and the pill self-dismisses
   after 1.5 s. On error the indicator turns into a red cross with the
   error message and stays visible for 5 s.

If Gemini returns an overload signal (HTTP 503 *UNAVAILABLE* or 429
*RESOURCE_EXHAUSTED*), the pill counts down `Gemini overloaded ·
retrying in Xs ...` second by second and then sends the request again.
Up to three attempts total — first retry after 5 s, second retry after
10 s. A third overload surfaces as the error message.

Press **ESC** at any time to abort an in-flight request. In the editor
this also closes the window; in direct mode it cancels via a system-
wide keyboard hook so it works without the pill stealing focus.

For longer text or quick experimentation, the last result row is
**"Open editor ..."** which opens a dedicated window with multi-line
input, an action picker, a result pane and a status line with its own
spinner. Shortcuts:

| Shortcut    | Effect                                       |
|-------------|----------------------------------------------|
| `Ctrl+Enter`| Run the selected action **and** copy result  |
| `Esc`       | Close the editor                             |

The Send button runs the action without auto-copying — use the Copy
button or `Ctrl+Enter` if you want it on the clipboard.

## Install

### Via Flow Launcher Plugin Store *(after manifest is merged)*

1. Open Flow Launcher → `flow plugins`.
2. Search for **Gemini Actions**.
3. Click **Install**.

### Manual install (release ZIP)

1. Download the latest `GeminiActions-<version>.zip` from the
   [releases page](https://github.com/haugjan/Flow.GeminiActions/releases/latest).
2. Open Flow Launcher → Settings → Plugins → **Install plugin from a
   local zip file**, pick the downloaded ZIP.
3. Restart Flow Launcher.

### From source

```powershell
git clone https://github.com/haugjan/Flow.GeminiActions
cd Flow.GeminiActions
.\Flow.GeminiActions\Start.ps1
```

`Start.ps1` stops Flow Launcher, builds the plugin in Debug, copies the
output into `%APPDATA%\FlowLauncher\Plugins\Gemini Actions-<version>`,
and relaunches Flow Launcher.

## Configuration

Open Flow Launcher → Settings → Plugins → **Gemini Actions**:

- **Gemini API Key** — get one from
  [aistudio.google.com](https://aistudio.google.com/app/apikey). Stored
  as a `PasswordBox` value in your Flow Launcher settings JSON.
- **Model** — defaults to `gemini-2.5-flash-lite`. Any model exposed by the
  `generativelanguage.googleapis.com/v1beta/models/{model}:generateContent`
  endpoint works.
- **Timeout** — request timeout in seconds (5–120).
- **Actions** — title, short description (shown as the result subtitle)
  and the full instruction text sent to Gemini. *Reset to defaults*
  restores the three built-in prompts.

## Default actions

| Title           | Purpose                                                                |
|-----------------|------------------------------------------------------------------------|
| Translate       | Translate the text into English, professional natural tone, no preamble. |
| Correct         | Fix grammar/spelling/style, keep the original language.                |
| Bullets to text | Turn bullet points into a cohesive professional text.                  |

## Privacy notes

- Result subtitles never echo the typed text or clipboard contents — if
  you happen to have an API key on the clipboard, it will not leak into
  the visible result list.
- The plugin sends the instruction and your input to Google's Gemini
  API. Read Google's
  [Gemini API terms](https://ai.google.dev/gemini-api/terms) for what
  Google does with that data.
- API key authentication uses the `x-goog-api-key` header, not URL
  query parameters, to keep the key out of HTTP referrer logs.

## Build & test

The plugin targets `net9.0-windows`. A `global.json` pins the build to
the .NET 9 SDK, so make sure it is installed.

```powershell
dotnet restore
dotnet build Flow.GeminiActions.slnx -c Release
dotnet test  Flow.GeminiActions.slnx
```

For producing an installable ZIP locally, run
`Flow.GeminiActions\Build-Plugin.ps1`.

The publish workflow tags releases from the `Version` field in
`Flow.GeminiActions/plugin.json` — bumping that field on `main` is what
triggers a new GitHub release.

## License

[MIT](LICENSE).

## Trademark

"Gemini" and the Gemini brand are trademarks of Google LLC. This plugin
is an independent, unofficial integration and is not affiliated with,
sponsored by, or endorsed by Google.
