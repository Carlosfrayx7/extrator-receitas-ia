namespace PrescriptionExtractor.Core.Domain;

public sealed record ExtractedField(string? Value, Confidence Confidence)
{
    public static ExtractedField Missing => new(null, Confidence.Low);
    public bool HasValue => !string.IsNullOrWhiteSpace(Value);
}
