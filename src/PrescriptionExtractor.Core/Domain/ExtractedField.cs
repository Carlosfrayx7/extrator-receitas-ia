using System.Text.Json.Serialization;

namespace PrescriptionExtractor.Core.Domain;

public sealed record ExtractedField(string? Value, Confidence Confidence)
{
    public static ExtractedField Missing => new(null, Confidence.Low);

    [JsonIgnore]
    public bool HasValue => !string.IsNullOrWhiteSpace(Value);
}
