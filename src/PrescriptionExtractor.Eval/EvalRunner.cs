using System.Collections.Concurrent;
using PrescriptionExtractor.Core.Domain;
using PrescriptionExtractor.Core.Extraction;

namespace PrescriptionExtractor.Eval;

public sealed record DocumentRun(string Document, string Kind, DocumentScore? Score, ExtractionResult? Result, string? Error);

public sealed record ModelRun(string Model, IReadOnlyList<DocumentRun> Documents)
{
    private IEnumerable<FieldScore> Fields => Documents.Where(d => d.Score is not null).SelectMany(d => d.Score!.Fields);
    private IEnumerable<ExtractionResult> Results => Documents.Where(d => d.Result is not null).Select(d => d.Result!);

    public double FieldAccuracy => Ratio(Fields.Count(f => f.Correct), Fields.Count());
    public double PerfectRate => Ratio(Documents.Count(d => d.Score?.Perfect == true), Documents.Count);
    public double RecognitionAccuracy => Ratio(Documents.Count(d => d.Score?.RecognizedCorrectly == true), Documents.Count);
    public double ValidJsonRate => Ratio(Documents.Count(d => d.Result is not null), Documents.Count);
    public int ErrorCount => Documents.Count(d => d.Error is not null);
    public decimal TotalCostUsd => Results.Sum(r => r.EstimatedCostUsd);
    public long TotalInputTokens => Results.Sum(r => r.InputTokens);
    public long TotalOutputTokens => Results.Sum(r => r.OutputTokens);
    public double AvgLatencyMs => Results.Any() ? Results.Average(r => r.LatencyMs) : 0;

    public double P95LatencyMs
    {
        get
        {
            var sorted = Results.Select(r => r.LatencyMs).Order().ToList();
            return sorted.Count == 0 ? 0 : sorted[(int)Math.Ceiling(0.95 * sorted.Count) - 1];
        }
    }

    public IReadOnlyDictionary<string, double> AccuracyByKind => Documents
        .Where(d => d.Score is not null && d.Kind != "other")
        .GroupBy(d => d.Kind)
        .ToDictionary(g => g.Key, g => Ratio(g.Sum(d => d.Score!.Correct), g.Sum(d => d.Score!.Total)));

    // Só campos em que o modelo declarou confiança (ActualConfidence nulo fica de fora).
    public IReadOnlyDictionary<Confidence, (int Total, double Accuracy)> AccuracyByConfidence => Fields
        .Where(f => f.ActualConfidence is not null)
        .GroupBy(f => f.ActualConfidence!.Value)
        .ToDictionary(g => g.Key, g => (g.Count(), Ratio(g.Count(f => f.Correct), g.Count())));

    private static double Ratio(int part, int total) => total == 0 ? 0 : (double)part / total;
}

public sealed class EvalRunner(IPrescriptionExtractor extractor, int parallelism = 4)
{
    public async Task<ModelRun> RunAsync(string datasetDir, string model, CancellationToken ct)
    {
        var documents = Directory.GetFiles(datasetDir)
            .Where(f => !f.EndsWith(".expected.json", StringComparison.Ordinal))
            .Order()
            .ToList();

        var runs = new ConcurrentBag<DocumentRun>();
        await Parallel.ForEachAsync(documents, new ParallelOptions { MaxDegreeOfParallelism = parallelism, CancellationToken = ct },
            async (path, token) => runs.Add(await RunOneAsync(path, model, token)));

        return new ModelRun(model, runs.OrderBy(r => r.Document, StringComparer.Ordinal).ToList());
    }

    private async Task<DocumentRun> RunOneAsync(string path, string model, CancellationToken ct)
    {
        var name = Path.GetFileName(path);
        var expected = PrescriptionJson.Parse(await File.ReadAllTextAsync(path + ".expected.json", ct));
        try
        {
            var mediaType = FileRules.ResolveMediaType(name, null);
            var result = await extractor.ExtractAsync(await File.ReadAllBytesAsync(path, ct), mediaType, model, ct);
            return new DocumentRun(name, KindOf(name), DocumentScorer.Score(name, expected, result.Prescription), result, null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new DocumentRun(name, KindOf(name), null, null, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private static string KindOf(string name) => name switch
    {
        _ when name.StartsWith("other-", StringComparison.Ordinal) => "other",
        _ when name.EndsWith("-typed.pdf", StringComparison.Ordinal) => "typed-pdf",
        _ when name.EndsWith("-typed.png", StringComparison.Ordinal) => "typed-png",
        _ => "handwritten",
    };
}
