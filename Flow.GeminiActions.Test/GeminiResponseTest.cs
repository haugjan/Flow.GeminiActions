using System.Text.Json;
using Flow.GeminiActions.GeminiClient;
using Shouldly;

namespace Flow.GeminiActions.Test;

public class GeminiResponseTest
{
    private static GeminiResponse Deserialize(string json) =>
        JsonSerializer.Deserialize<GeminiResponse>(json)!;

    [Fact]
    public void FirstText_ReturnsSinglePartText()
    {
        var r = Deserialize("""{"candidates":[{"content":{"parts":[{"text":"Hello"}]}}]}""");
        r.FirstText().ShouldBe("Hello");
    }

    [Fact]
    public void FirstText_ConcatenatesMultipleNonThoughtParts()
    {
        var r = Deserialize(
            """{"candidates":[{"content":{"parts":[{"text":"Hel"},{"text":"lo"}]}}]}"""
        );
        r.FirstText().ShouldBe("Hello");
    }

    [Fact]
    public void FirstText_FiltersOutThoughtParts()
    {
        var r = Deserialize(
            """{"candidates":[{"content":{"parts":[{"text":"[thinking]","thought":true},{"text":"Hello"}]}}]}"""
        );
        r.FirstText().ShouldBe("Hello");
    }

    [Fact]
    public void FirstText_WhenOnlyThoughtPartsExist_FallsBackToAllParts()
    {
        // If thinkingBudget:0 is ignored by the model and only thought content is
        // returned, the fallback returns the thought text rather than nothing.
        var r = Deserialize(
            """{"candidates":[{"content":{"parts":[{"text":"some thought","thought":true}]}}]}"""
        );
        r.FirstText().ShouldBe("some thought");
    }

    [Fact]
    public void FirstText_ReturnsNullForEmptyPartsList()
    {
        var r = Deserialize("""{"candidates":[{"content":{"parts":[]}}]}""");
        r.FirstText().ShouldBeNull();
    }

    [Fact]
    public void FirstText_ReturnsNullForNoCandidates()
    {
        var r = Deserialize("""{"candidates":[]}""");
        r.FirstText().ShouldBeNull();
    }

    [Fact]
    public void FirstText_ReturnsNullForNullCandidates()
    {
        var r = Deserialize("""{}""");
        r.FirstText().ShouldBeNull();
    }

    [Fact]
    public void GeminiPart_ThoughtFalse_IsNotSerializedInRequest()
    {
        // The "thought" field must not appear in outgoing requests since it is
        // a response-only field. JsonIgnore(WhenWritingDefault) suppresses it.
        var part = new GeminiPart(Text: "hello");
        var json = JsonSerializer.Serialize(part);
        json.ShouldContain("hello");
        json.ShouldNotContain("thought");
    }

    [Fact]
    public void GeminiPart_ThoughtTrue_IsSerializedWhenExplicitlySet()
    {
        var part = new GeminiPart(Text: "thinking...", Thought: true);
        var json = JsonSerializer.Serialize(part);
        json.ShouldContain("thought");
        json.ShouldContain("true");
    }
}
