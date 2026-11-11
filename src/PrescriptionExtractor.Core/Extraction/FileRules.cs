using System.Text;
using System.Text.RegularExpressions;

namespace PrescriptionExtractor.Core.Extraction;

public static partial class FileRules
{
    public const long MaxBytes = 10 * 1024 * 1024;
    public const int MaxPdfPages = 5;

    [GeneratedRegex(@"/Type\s*/Page(?!s)")]
    private static partial Regex PageObject();

    public static string ResolveMediaType(string fileName, string? contentType)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return (ext, contentType?.ToLowerInvariant()) switch
        {
            (".png", _) or ("", "image/png") => "image/png",
            (".jpg" or ".jpeg", _) or ("", "image/jpeg") => "image/jpeg",
            (".pdf", _) or ("", "application/pdf") => "application/pdf",
            _ => throw new UnsupportedFileException("Envie uma imagem PNG ou JPEG ou um PDF."),
        };
    }

    public static void EnsureValid(byte[] data, string mediaType)
    {
        if (data.Length == 0) throw new UnsupportedFileException("O arquivo está vazio.");
        if (data.Length > MaxBytes) throw new UnsupportedFileException("O arquivo passa de 10 MB.");
        if (mediaType == "application/pdf" && PageObject().Matches(Encoding.Latin1.GetString(data)).Count > MaxPdfPages)
            throw new UnsupportedFileException($"O PDF passa de {MaxPdfPages} páginas.");
    }
}
