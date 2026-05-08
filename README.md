# Flow.GeminiActions

Flow Launcher plugin that runs your text through pre-configured Gemini
prompts (translate, correct, rewrite, …) and copies the result to the
clipboard.

Triggered with the `ask` action keyword.

## How it works

1. Type `ask <text>` in Flow Launcher — or copy text to the clipboard and
   trigger `ask` with no arguments.
2. Pick one of the configured actions (Translate, Correct, Bullets to
   text, plus any custom shortcuts you add).
3. The plugin sends `<instruction>\n\n---\n<text>` to Gemini, copies the
   response to the clipboard, and shows a toast.

## Configuration

Open Flow Launcher → Settings → Plugins → Gemini Actions:

- **Gemini API Key** — get one from
  [aistudio.google.com](https://aistudio.google.com/app/apikey).
- **Model** — defaults to `gemini-2.5-flash`. Any model exposed by the
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

## Build & run locally

```powershell
dotnet restore
dotnet build Flow.GeminiActions.sln -c Release
```

For interactive plugin development, run `Flow.GeminiActions\Start.ps1`
from the project directory — it stops Flow Launcher, rebuilds, copies the
DLLs into `%APPDATA%\FlowLauncher\Plugins\Gemini Actions-<version>` and
relaunches.

For producing an installable ZIP, run `Flow.GeminiActions\Build-Plugin.ps1`.

## Publishing

The publish workflow tags releases from the `Version` field in
`Flow.GeminiActions/plugin.json` — bumping that field on `main` is what
triggers a new GitHub release.
