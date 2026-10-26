using Microsoft.Extensions.Configuration;

namespace HoneKit.Requirements.Parsing;

/// <summary>The AI configuration as the reader sees it. The key is used, never shown.</summary>
public sealed record AiSettings(string Provider, string Model, string Endpoint, string ApiKey)
{
    public static AiSettings From(IConfiguration configuration) => new(
        (configuration["AI:PROVIDER"] ?? "fixture").Trim().ToLowerInvariant(),
        configuration["AI:MODEL"] ?? "gpt-5-mini",
        configuration["AI:ENDPOINT"] ?? "https://api.openai.com/v1",
        configuration["AI:APIKEY"] ?? "");
}
