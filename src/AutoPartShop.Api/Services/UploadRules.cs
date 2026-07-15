namespace AutoPartShop.Api.Services;

/// <summary>
/// Per-file-type upload rules: allowed extensions, server-side content type,
/// size limit, and magic-byte signatures. The kind (IMAGE/VIDEO/DOCUMENT) is
/// inferred from the extension; the client's content type is never trusted.
/// </summary>
public sealed class UploadRule
{
    public required string Kind { get; init; }          // IMAGE, VIDEO, DOCUMENT
    public required string ContentType { get; init; }
    public required long MaxBytes { get; init; }
    /// <summary>Accepted signatures as (offset, bytes) pairs; empty = no signature check (plain text).</summary>
    public (int Offset, byte[] Bytes)[][] Signatures { get; init; } = [];
}

public static class UploadRules
{
    private const long MaxImageBytes = 5 * 1024 * 1024;      // 5 MB
    private const long MaxVideoBytes = 100 * 1024 * 1024;    // 100 MB
    private const long MaxDocumentBytes = 10 * 1024 * 1024;  // 10 MB

    /// <summary>Largest single request the upload endpoint accepts (video limit + multipart overhead).</summary>
    public const long MaxRequestBytes = MaxVideoBytes + 1024 * 1024;

    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47];
    private static readonly byte[] Gif = [0x47, 0x49, 0x46];
    private static readonly byte[] Riff = [0x52, 0x49, 0x46, 0x46];              // "RIFF"
    private static readonly byte[] Webp = [0x57, 0x45, 0x42, 0x50];              // "WEBP" @ 8
    private static readonly byte[] Ftyp = [0x66, 0x74, 0x79, 0x70];              // "ftyp" @ 4 (mp4/mov)
    private static readonly byte[] Ebml = [0x1A, 0x45, 0xDF, 0xA3];              // webm/mkv
    private static readonly byte[] Pdf = [0x25, 0x50, 0x44, 0x46];               // "%PDF"
    private static readonly byte[] OleCf = [0xD0, 0xCF, 0x11, 0xE0];             // legacy .doc/.xls
    private static readonly byte[] Zip = [0x50, 0x4B];                           // .docx/.xlsx (OOXML)

    private static readonly Dictionary<string, UploadRule> Rules = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = new() { Kind = "IMAGE", ContentType = "image/jpeg", MaxBytes = MaxImageBytes, Signatures = [[(0, Jpeg)]] },
        [".jpeg"] = new() { Kind = "IMAGE", ContentType = "image/jpeg", MaxBytes = MaxImageBytes, Signatures = [[(0, Jpeg)]] },
        [".png"] = new() { Kind = "IMAGE", ContentType = "image/png", MaxBytes = MaxImageBytes, Signatures = [[(0, Png)]] },
        [".gif"] = new() { Kind = "IMAGE", ContentType = "image/gif", MaxBytes = MaxImageBytes, Signatures = [[(0, Gif)]] },
        [".webp"] = new() { Kind = "IMAGE", ContentType = "image/webp", MaxBytes = MaxImageBytes, Signatures = [[(0, Riff), (8, Webp)]] },

        [".mp4"] = new() { Kind = "VIDEO", ContentType = "video/mp4", MaxBytes = MaxVideoBytes, Signatures = [[(4, Ftyp)]] },
        [".mov"] = new() { Kind = "VIDEO", ContentType = "video/quicktime", MaxBytes = MaxVideoBytes, Signatures = [[(4, Ftyp)]] },
        [".webm"] = new() { Kind = "VIDEO", ContentType = "video/webm", MaxBytes = MaxVideoBytes, Signatures = [[(0, Ebml)]] },

        [".pdf"] = new() { Kind = "DOCUMENT", ContentType = "application/pdf", MaxBytes = MaxDocumentBytes, Signatures = [[(0, Pdf)]] },
        [".doc"] = new() { Kind = "DOCUMENT", ContentType = "application/msword", MaxBytes = MaxDocumentBytes, Signatures = [[(0, OleCf)]] },
        [".docx"] = new() { Kind = "DOCUMENT", ContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document", MaxBytes = MaxDocumentBytes, Signatures = [[(0, Zip)]] },
        [".xls"] = new() { Kind = "DOCUMENT", ContentType = "application/vnd.ms-excel", MaxBytes = MaxDocumentBytes, Signatures = [[(0, OleCf)]] },
        [".xlsx"] = new() { Kind = "DOCUMENT", ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", MaxBytes = MaxDocumentBytes, Signatures = [[(0, Zip)]] },
        [".csv"] = new() { Kind = "DOCUMENT", ContentType = "text/csv", MaxBytes = MaxDocumentBytes },
        [".txt"] = new() { Kind = "DOCUMENT", ContentType = "text/plain", MaxBytes = MaxDocumentBytes },
    };

    public static string AllowedExtensions => string.Join(", ", Rules.Keys.OrderBy(x => x));

    public static UploadRule? Resolve(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        return !string.IsNullOrEmpty(extension) && Rules.TryGetValue(extension, out var rule) ? rule : null;
    }

    /// <summary>Checks the file header against the rule's magic-byte signatures (any match passes).</summary>
    public static async Task<bool> MatchesSignatureAsync(UploadRule rule, Stream content, CancellationToken cancellationToken)
    {
        if (rule.Signatures.Length == 0)
            return true;

        var headerLength = rule.Signatures.Max(s => s.Max(p => p.Offset + p.Bytes.Length));
        var header = new byte[headerLength];
        var read = await content.ReadAtLeastAsync(header, headerLength, throwOnEndOfStream: false, cancellationToken);

        return rule.Signatures.Any(signature => signature.All(part =>
            part.Offset + part.Bytes.Length <= read &&
            header.AsSpan(part.Offset, part.Bytes.Length).SequenceEqual(part.Bytes)));
    }
}
