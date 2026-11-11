using PrescriptionExtractor.Core.Domain;
using PrescriptionExtractor.Core.Validation;

namespace PrescriptionExtractor.Core.Extraction;

public sealed record ExtractionResult(
    Prescription Prescription, ExtractionStatus Status, IReadOnlyList<ValidationIssue> Issues, string Model,
    long InputTokens, long OutputTokens, decimal EstimatedCostUsd, long LatencyMs);

public interface IPrescriptionExtractor
{
    Task<ExtractionResult> ExtractAsync(byte[] file, string mediaType, string? model, CancellationToken ct);
}
