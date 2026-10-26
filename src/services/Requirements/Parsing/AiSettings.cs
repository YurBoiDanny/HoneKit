using Microsoft.Extensions.Configuration;

namespace HoneKit.Requirements.Parsing;

/// <summary>The AI configuration as the reader sees it. The key is used, never shown.</summary>
public sealed record AiSettings(string Provider, string Model, string Endpoint, string ApiKey)
{
    public static AiSettings From(IConfiguration configuration)
    {
        var provider = (configuration["AI:PROVIDER"] ?? "fixture").Trim().ToLowerInvariant();
        var isGo = provider is "opencode-go" or "opencode_go";

        return new AiSettings(
            isGo ? "opencode-go" : provider,
            configuration["AI:MODEL"] ?? (isGo ? "glm-5.3-flash" : "gpt-5-mini"),
            configuration["AI:ENDPOINT"]
                ?? (isGo ? "https://opencode.ai/inference/go/openai/v1" : "https://api.openai.com/v1"),
            configuration["AI:APIKEY"] ?? "");
    }
}
