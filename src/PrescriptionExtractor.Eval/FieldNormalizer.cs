using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace PrescriptionExtractor.Eval;

public static partial class FieldNormalizer
{
    [GeneratedRegex(@"(\d)\s*(mg|g|mcg|ml|ui|%)(?![\p{L}\d])")]
    private static partial Regex NumberUnit();

    [GeneratedRegex(@"(?<!\d)\.|\.(?!\d)")]
    private static partial Regex NonDecimalDot();

    [GeneratedRegex(@"[^\w\s%.]")]
    private static partial Regex Punctuation();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Spaces();

    [GeneratedRegex(@"(\d),(\d)")]
    private static partial Regex DecimalComma();

    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var s = RemoveAccents(value.Trim().ToLowerInvariant());
        s = DecimalComma().Replace(s, "$1.$2");
        s = NonDecimalDot().Replace(s, " ");
        s = Punctuation().Replace(s, " ");
        s = NumberUnit().Replace(s, "$1 $2");
        s = Spaces().Replace(s, " ").Trim().TrimEnd('.');
        return s.Length == 0 ? null : s;
    }

    public static bool Equivalent(string? expected, string? actual) => Normalize(expected) == Normalize(actual);

    private static string RemoveAccents(string s)
    {
        var sb = new StringBuilder(s.Length);
        foreach (var c in s.Normalize(NormalizationForm.FormD))
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}
