namespace PrescriptionExtractor.Core.Extraction;

public static class ModelPricing
{
    private static readonly Dictionary<string, (decimal Input, decimal Output)> PerMillion = new()
    {
        [ClaudeModels.Opus] = (4.00m, 20.00m),
        [ClaudeModels.Haiku] = (0.10m, 0.50m),
    };

    public static decimal EstimateUsd(string model, long inputTokens, long outputTokens)
    {
        var (input, output) = PerMillion.TryGetValue(model, out var p) ? p : throw new UnsupportedModelException(model);
        return Math.Round(inputTokens * input / 1_000_000m + outputTokens * output / 1_000_000m, 6);
    }
}
