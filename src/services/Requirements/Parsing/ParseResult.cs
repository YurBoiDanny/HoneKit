using HoneKit.Contracts;
using HoneKit.Requirements.Domain;

namespace HoneKit.Requirements.Parsing;

/// <summary>A failure with the HTTP status the endpoint should use: 503 unavailable, 422 unreadable.</summary>
public sealed record ParseFailure(int StatusCode, ParseFailureDto Body);

/// <summary>The parser's answer: a normalized draft, or a body the leader can act on.</summary>
public sealed record ParseResult(ParsedRequirements? Draft, string Provider, string Model, ParseFailure? Failure)
{
    public bool Succeeded => Failure is null;

    public static ParseResult Success(ParsedRequirements draft, string provider, string model) =>
        new(draft, provider, model, null);

    public static ParseResult Failed(
        int statusCode,
        string detail,
        string sourceName,
        string sourceText,
        IReadOnlyList<string> errors,
        string provider = "",
        string model = "") =>
        new(
            null,
            provider,
            model,
            new ParseFailure(statusCode, new ParseFailureDto(detail, sourceName, sourceText, errors)));
}
