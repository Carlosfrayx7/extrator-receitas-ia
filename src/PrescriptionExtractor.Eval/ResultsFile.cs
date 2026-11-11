using System.Text.Json;
using PrescriptionExtractor.Core.Domain;

namespace PrescriptionExtractor.Eval;

// DTO do JSON bruto da avaliação (sem valores extraídos, para manter o arquivo pequeno).
public sealed record CalibrationBucket(int Total, double Accuracy);

public sealed record DocumentResultDto(string Name, string Kind, int? Correct, int? Total, bool Perfect, bool Recognized,
    string? Error, long? InputTokens, long? OutputTokens, decimal? CostUsd, long? LatencyMs);

public sealed record ModelResultDto(string Model, double FieldAccuracy, double PerfectRate, double? RecognitionAccuracy,
    double ValidJsonRate, int ErrorCount, IReadOnlyDictionary<string, double> AccuracyByKind,
    IReadOnlyDictionary<string, CalibrationBucket> Calibration, double AvgLatencyMs, double P95LatencyMs,
    decimal TotalCostUsd, long TotalInputTokens, long TotalOutputTokens, IReadOnlyList<DocumentResultDto> Documents);

public static class ResultsFile
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public static string Serialize(IReadOnlyList<ModelRun> runs) => JsonSerializer.Serialize(runs.Select(ToDto), Options);

    private static ModelResultDto ToDto(ModelRun r) => new(
        r.Model, r.FieldAccuracy, r.PerfectRate, r.NonPrescriptionRecognition, r.ValidJsonRate, r.ErrorCount, r.AccuracyByKind,
        r.AccuracyByConfidence.ToDictionary(kv => ConfidenceName(kv.Key), kv => new CalibrationBucket(kv.Value.Total, kv.Value.Accuracy)),
        r.AvgLatencyMs, r.P95LatencyMs, r.TotalCostUsd, r.TotalInputTokens, r.TotalOutputTokens,
        r.Documents.Select(d => new DocumentResultDto(d.Document, d.Kind, d.Score?.Correct, d.Score?.Total,
            d.Score?.Perfect == true, d.Score?.RecognizedCorrectly == true, d.Error,
            d.Result?.InputTokens, d.Result?.OutputTokens, d.Result?.EstimatedCostUsd, d.Result?.LatencyMs)).ToList());

    private static string ConfidenceName(Confidence c) => c.ToString().ToLowerInvariant();
}
