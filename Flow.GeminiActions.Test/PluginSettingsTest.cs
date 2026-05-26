using Flow.GeminiActions.Settings;
using Shouldly;

namespace Flow.GeminiActions.Test;

public class PluginSettingsTest
{
    [Fact]
    public void Constructor_SeedsThreeDefaultActions()
    {
        var settings = new PluginSettings();

        settings.Actions.Count.ShouldBe(3);
        settings.Actions.Select(a => a.Title).ShouldBe(["Translate", "Correct", "Bullets to text"]);
    }

    [Fact]
    public void Constructor_DefaultsToFlashLiteModel()
    {
        var settings = new PluginSettings();

        settings.Model.ShouldBe("gemini-2.5-flash-lite");
    }

    [Fact]
    public void DefaultActions_AllHaveTitleAndInstruction()
    {
        var actions = PluginSettings.DefaultActions();

        actions.ShouldAllBe(a =>
            !string.IsNullOrWhiteSpace(a.Title) && !string.IsNullOrWhiteSpace(a.Instruction)
        );
    }

    [Fact]
    public void DefaultActions_ReturnsFreshList()
    {
        var first = PluginSettings.DefaultActions();
        var second = PluginSettings.DefaultActions();

        first.ShouldNotBeSameAs(second);
    }
}
