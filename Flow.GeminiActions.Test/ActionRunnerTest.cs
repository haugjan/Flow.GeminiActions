using Flow.GeminiActions.Actions;
using Flow.GeminiActions.Settings;
using Flow.Launcher.Plugin;
using NSubstitute;
using Shouldly;

namespace Flow.GeminiActions.Test;

public class ActionRunnerTest
{
    private static (ActionRunner runner, IResultCreator creator) Build(PluginSettings settings)
    {
        var creator = Substitute.For<IResultCreator>();
        creator
            .CreateActionResult(Arg.Any<GeminiAction>(), Arg.Any<Func<string>>())
            .Returns(call => new Result { Title = ((GeminiAction)call[0]).Title });
        creator
            .CreateOpenEditorResult(Arg.Any<Func<string>>())
            .Returns(_ => new Result { Title = "Open editor ..." });
        creator
            .CreateHint(Arg.Any<string>(), Arg.Any<string>())
            .Returns(call => new Result { Title = (string)call[0], SubTitle = (string)call[1] });
        creator
            .CreateError(Arg.Any<string>(), Arg.Any<string>())
            .Returns(call => new Result { Title = (string)call[0], SubTitle = (string)call[1] });

        return (new ActionRunner(settings, creator), creator);
    }

    [Fact]
    public async Task TypedText_WithApiKey_ReturnsActionResultsAndAppendsEditor()
    {
        var settings = new PluginSettings { ApiKey = "key" };
        var (runner, _) = Build(settings);

        var results = await runner.QueryAsync(
            "hello",
            "ask",
            TestContext.Current.CancellationToken
        );

        // Hint row prepended when typed text is present
        results.First().Title.ShouldContain("Using typed text");
        results
            .Skip(1)
            .Select(r => r.Title)
            .ShouldBe(["Translate", "Correct", "Bullets to text", "Open editor ..."]);
    }

    [Fact]
    public async Task TypedText_WithoutApiKey_ReturnsConfigurationError()
    {
        var settings = new PluginSettings { ApiKey = string.Empty };
        var (runner, _) = Build(settings);

        var results = await runner.QueryAsync(
            "hello",
            "ask",
            TestContext.Current.CancellationToken
        );

        results.Count.ShouldBe(1);
        results[0].Title.ShouldContain("No Gemini API key");
    }

    [Fact]
    public async Task TypedText_WithEmptyActionsList_ReturnsConfigurationError()
    {
        var settings = new PluginSettings { ApiKey = "key", Actions = [] };
        var (runner, _) = Build(settings);

        var results = await runner.QueryAsync(
            "hello",
            "ask",
            TestContext.Current.CancellationToken
        );

        results.Count.ShouldBe(1);
        results[0].Title.ShouldContain("No actions defined");
    }

    [Fact]
    public async Task TypedText_FiltersOutActionsMissingTitleOrInstruction()
    {
        var settings = new PluginSettings
        {
            ApiKey = "key",
            Actions =
            [
                new GeminiAction { Title = "Good", Instruction = "Do this" },
                new GeminiAction { Title = string.Empty, Instruction = "Missing title" },
                new GeminiAction { Title = "Missing instruction", Instruction = string.Empty },
            ],
        };
        var (runner, _) = Build(settings);

        var results = await runner.QueryAsync(
            "hello",
            "ask",
            TestContext.Current.CancellationToken
        );

        results.First().Title.ShouldContain("Using typed text");
        results.Skip(1).Select(r => r.Title).ShouldBe(["Good", "Open editor ..."]);
    }

    [Fact]
    public async Task TypedText_DoesNotIncludeQueryContentInResultSubtitles()
    {
        // Regression guard: subtitles must never echo user input or clipboard contents.
        var settings = new PluginSettings { ApiKey = "key" };
        var (runner, creator) = Build(settings);

        const string secret = "supersecret-api-key-AIzaSy123";
        await runner.QueryAsync(secret, "ask", TestContext.Current.CancellationToken);

        creator
            .Received(3)
            .CreateActionResult(
                Arg.Any<GeminiAction>(),
                Arg.Is<Func<string>>(provider => provider() == secret)
            );
        creator
            .DidNotReceive()
            .CreateHint(Arg.Is<string>(s => s.Contains(secret)), Arg.Any<string>());
        creator
            .DidNotReceive()
            .CreateHint(Arg.Any<string>(), Arg.Is<string>(s => s.Contains(secret)));
    }

    [Fact]
    public async Task TypedText_PassesTextProviderResolvingToTheTypedText()
    {
        // The action result must carry a provider, not a pre-captured string,
        // so the clipboard fallback can re-read at action time. For typed text
        // the provider resolves to that text every time it is called.
        var settings = new PluginSettings { ApiKey = "key" };
        var (runner, creator) = Build(settings);

        await runner.QueryAsync("translate me", "ask", TestContext.Current.CancellationToken);

        var provider = (Func<string>)
            creator.ReceivedCalls().First(c => c.GetMethodInfo().Name == "CreateActionResult")
                .GetArguments()[1]!;
        provider().ShouldBe("translate me");
        provider().ShouldBe("translate me");
    }
}
