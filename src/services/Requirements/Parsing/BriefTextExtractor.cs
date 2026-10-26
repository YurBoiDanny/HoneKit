using System.Text;
using Microsoft.AspNetCore.Http;
using UglyToad.PdfPig;

namespace HoneKit.Requirements.Parsing;

/// <summary>Extracted text, or why the upload could not be read.</summary>
public sealed record BriefExtraction(bool Succeeded, string Text, string Detail, int StatusCode)
{
    public static BriefExtraction Success(string text) => new(true, text, "", StatusCodes.Status200OK);

    public static BriefExtraction Failure(int statusCode, string detail) => new(false, "", detail, statusCode);
}

/// <summary>File intake: three extensions, a 5 MB cap, UTF-8 text, and PdfPig for text PDFs.</summary>
public static class BriefTextExtractor
{
    public const long MaxBytes = 5 * 1024 * 1024;

    private static readonly string[] AllowedExtensions = [".txt", ".md", ".pdf"];

    public static async Task<BriefExtraction> ExtractAsync(IFormFile file, CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!AllowedExtensions.Contains(extension))
        {
            return BriefExtraction.Failure(StatusCodes.Status415UnsupportedMediaType, "Upload a .txt, .md or .pdf file.");
        }

        if (file.Length > MaxBytes)
        {
            return BriefExtraction.Failure(StatusCodes.Status413PayloadTooLarge, "The file is larger than 5 MB.");
        }

        if (file.Length == 0)
        {
            return BriefExtraction.Failure(StatusCodes.Status422UnprocessableEntity, "That file is empty.");
        }

        using var stream = file.OpenReadStream();

        if (extension == ".pdf")
        {
            return ExtractPdf(stream);
        }

        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var text = await reader.ReadToEndAsync(cancellationToken);

        return string.IsNullOrWhiteSpace(text)
            ? BriefExtraction.Failure(StatusCodes.Status422UnprocessableEntity, "That file has no text to read.")
            : BriefExtraction.Success(text);
    }

    private static BriefExtraction ExtractPdf(Stream stream)
    {
        try
        {
            // A small buffered copy keeps the PDF library off the request stream and is seekable.
            using var buffer = new MemoryStream();
            stream.CopyTo(buffer);
            buffer.Position = 0;

            using var document = PdfDocument.Open(buffer);

            var text = new StringBuilder();
            foreach (var page in document.GetPages())
            {
                text.AppendLine(page.Text);
            }

            return string.IsNullOrWhiteSpace(text.ToString())
                ? BriefExtraction.Failure(
                    StatusCodes.Status422UnprocessableEntity,
                    "This PDF looks like a scan — paste the text instead.")
                : BriefExtraction.Success(text.ToString());
        }
        catch (Exception)
        {
            return BriefExtraction.Failure(
                StatusCodes.Status422UnprocessableEntity,
                "That PDF could not be read. Paste the text instead.");
        }
    }
}
