# Flow.GeminiActions

Flow Launcher plugin (C# / .NET 9, WPF) that runs user-configurable
Gemini prompts against text and copies the response to the clipboard.
Triggered with the `ask` action keyword.

## Repository layout

- `Flow.GeminiActions/` — plugin assembly
  - `Main.cs` — entry point, implements `IAsyncPlugin`, `ISettingProvider`
  - `ServiceProvider.cs` — DI registration (`Microsoft.Extensions.DependencyInjection`)
  - `GeminiClient/`
    - `GeminiClient.cs` — POSTs to
      `https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent`
      and returns the first candidate text
    - `GeminiRequest.cs`, `GeminiResponse.cs` — DTOs for the JSON
      payloads
  - `Actions/`
    - `ActionRunner.cs` — turns the user query (or clipboard) into a list
      of `Result`s, one per configured action
    - `ResultCreator.cs` — builds `Result` items; `Action` fires the
      Gemini call on a background `Task`, copies to clipboard via the
      WPF dispatcher, and surfaces a toast through `context.API.ShowMsg`
  - `Settings/` — WPF settings panel
    - `PluginSettings.cs` — `ApiKey`, `Model`, `Timeout`, `Actions`;
      `DefaultActions()` seeds Translate / Correct / Bullets-to-text
    - `GeminiAction.cs` — `Title`, `Description`, `Instruction`
    - `SettingsView.xaml(.cs)`, `SettingsViewModel.cs`, `Configurator.cs`
  - `plugin.json` — Flow Launcher manifest (action keyword `ask`, ID,
    version, icon)
  - `Editor/EditorWindow.xaml(.cs)` — separate WPF window opened from
    the "Open editor ..." result row; multi-line input + action picker +
    read-only result; ESC closes, Ctrl+Enter sends and copies the
    response to the clipboard (the Send button does not auto-copy)
  - `Build-Plugin.ps1` — packages the plugin into a ZIP for manual install
  - `Start.ps1` — local dev helper: stops Flow Launcher, builds, copies
    the output into `%APPDATA%\FlowLauncher\Plugins\Gemini Actions-<version>`,
    restarts Flow Launcher
- `Flow.GeminiActions.Test/` — xUnit v3 + Shouldly + NSubstitute tests.
  Requires the `xunit.runner.visualstudio` adapter package next to
  `xunit.v3` so that `dotnet test` can discover the tests; without it
  VSTest reports "No test is available".
- `.github/workflows/`
  - `build-action.yml` — PR build (`dotnet publish` win-x64, uploads
    artifact `GeminiActions-<version>`)
  - `publish-action.yml` — release on push to `main`, tags
    `v<plugin.json Version>`, attaches the published ZIP
- `global.json` — pins the build to the .NET 9 SDK (see Build & test).

## Build & test

```powershell
dotnet restore
dotnet build Flow.GeminiActions.slnx -c Release
dotnet test  Flow.GeminiActions.slnx
```

The solution targets `net9.0-windows` and `global.json` pins the build
to the .NET 9 SDK. Building with a .NET 10 SDK selects a newer .NET 9
apphost pack (e.g. `9.0.16`) that is usually not installed locally and
cannot always be restored on a locked-down network; the test project,
which xUnit v3 forces to build an app host, then fails with
`MSB3030 ("apphost.exe ... was not found")`. Keeping the build on a
.NET 9 SDK uses the apphost pack that ships with the installed runtime.

For interactive plugin development, run `Flow.GeminiActions\Start.ps1`
from the project directory.

For producing an installable ZIP, run
`Flow.GeminiActions\Build-Plugin.ps1`.

The publish workflow tags releases from the `Version` field in
`Flow.GeminiActions/plugin.json` — bumping that field on `main` is what
triggers a new GitHub release.

## Behaviour notes

- If the user types text after `ask`, that text is sent to Gemini.
  If the query is empty, `ActionRunner` falls back to the clipboard text
  (silently — a hint row tells the user it's using the clipboard).
- `Result.Action` returns immediately (`true`) so Flow Launcher hides
  itself; the Gemini call runs on `Task.Run`. The toast surfaces success
  or any thrown exception.
- The Gemini prompt is `<instruction>\n\n---\n<text>` — the `---`
  separator keeps the instruction and the source text distinguishable to
  the model.
- `HttpClient` is created per call via a `Func<HttpClient>` factory
  because `ApiKey`, `Timeout` and `Model` come from settings the user
  can edit at runtime — capturing a single client would freeze stale
  values.
- Auth is `x-goog-api-key: <key>`. `?key=` query-string auth would also
  work but the header keeps the key out of URLs / logs.

## Conventions

- Target framework: `net9.0-windows`, nullable enabled, WPF (`UseWpf`).
- Most types are `internal`.
- Code style follows the default .NET conventions; primary constructors
  are used throughout for DI.
- `Build-Plugin.ps1` and `Start.ps1` derive plugin id, version and
  folder name from `plugin.json` at runtime — don't add hard-coded
  versions when extending those scripts.

## Branch naming

The global `feature/firefly/OPA-…` / `hotfix/firefly/SUP-…` rules in
`~/.claude/CLAUDE.md` are scoped to firefly work and do **not** apply to
this personal repo. Use short `feature/<topic>` or `fix/<topic>` branch
names. Commit subjects are plain imperative sentences with no ticket
prefix.
