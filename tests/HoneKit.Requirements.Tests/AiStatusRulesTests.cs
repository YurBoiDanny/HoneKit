using HoneKit.Requirements.Domain;

namespace HoneKit.Requirements.Tests;

// What the status strip says. No network, no key, no probe: the live check is exercised by hand.
public sealed class AiStatusRulesTests
{
    [Fact]
    public void Fixture_is_ready_offline()
    {
        var status = AiStatusRules.Evaluate("fixture", "gpt-5-mini", "");

        Assert.Equal("fixture", status.Mode);
        Assert.True(status.KeyConfigured);
        Assert.Contains("no network", status.Detail, StringComparison.Ordinal);
        Assert.False(AiStatusRules.CanProbe(status.Mode));
    }

    [Fact]
    public void Live_with_a_key_is_ready()
    {
        var status = AiStatusRules.Evaluate("openai", "gpt-5-mini", "sk-secret");

        Assert.Equal("live", status.Mode);
        Assert.True(status.KeyConfigured);
        Assert.Contains("gpt-5-mini", status.Detail, StringComparison.Ordinal);
        Assert.DoesNotContain("sk-secret", status.Detail, StringComparison.Ordinal);
        Assert.True(AiStatusRules.CanProbe(status.Mode));
    }

    [Fact]
    public void Live_without_a_key_is_unavailable()
    {
        var status = AiStatusRules.Evaluate("openai", "gpt-5-mini", "");

        Assert.Equal("unavailable", status.Mode);
        Assert.False(status.KeyConfigured);
        Assert.Contains("no key", status.Detail, StringComparison.Ordinal);
        Assert.False(AiStatusRules.CanProbe(status.Mode));
    }

    [Fact]
    public void Ollama_needs_no_key()
    {
        var status = AiStatusRules.Evaluate("ollama", "llama3.2:3b", "");

        Assert.Equal("live", status.Mode);
        Assert.True(status.KeyConfigured);
        Assert.Contains("llama3.2:3b", status.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public void Broken_is_unavailable()
    {
        var status = AiStatusRules.Evaluate("broken", "gpt-5-mini", "");

        Assert.Equal("unavailable", status.Mode);
        Assert.Contains("switched off", status.Detail, StringComparison.Ordinal);
    }
}
