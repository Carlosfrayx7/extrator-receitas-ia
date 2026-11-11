using System.Text.Json;
using PrescriptionExtractor.Core.Domain;
using PrescriptionExtractor.Core.Validation;

namespace PrescriptionExtractor.Core.Extraction;

public sealed class ClaudePrescriptionExtractor(
    IMessagesGateway gateway, PrescriptionValidator validator, TimeProvider clock) : IPrescriptionExtractor
{
    public const int MaxTokens = 4096;
    public const string Effort = "medium";

    public async Task<ExtractionResult> ExtractAsync(byte[] file, string mediaType, string? model, CancellationToken ct)
    {
        FileRules.EnsureValid(file, mediaType);
        var modelId = ClaudeModels.Resolve(model);

        var started = clock.GetTimestamp();
        var response = await gateway.SendAsync(new GatewayRequest(
            modelId, PrescriptionSchema.SystemPrompt, PrescriptionSchema.UserInstruction, file, mediaType,
            PrescriptionSchema.Schema, MaxTokens, Effort), ct);
        var latencyMs = (long)clock.GetElapsedTime(started).TotalMilliseconds;

        if (response.StopReason == "refusal") throw new ModelRefusalException(response.RefusalCategory);
        if (response.StopReason == "max_tokens") throw new OutputTruncatedException();
        if (string.IsNullOrWhiteSpace(response.Text))
            throw new InvalidModelOutputException("O modelo não devolveu conteúdo.");

        Prescription prescription;
        try
        {
            prescription = PrescriptionJson.Parse(response.Text);
        }
        catch (JsonException ex)
        {
            throw new InvalidModelOutputException("O modelo devolveu um JSON fora do esquema.", ex);
        }

        var report = validator.Validate(prescription);
        return new ExtractionResult(
            prescription, report.Status, report.Issues, modelId,
            response.InputTokens, response.OutputTokens,
            ModelPricing.EstimateUsd(modelId, response.InputTokens, response.OutputTokens),
            latencyMs);
    }
}
