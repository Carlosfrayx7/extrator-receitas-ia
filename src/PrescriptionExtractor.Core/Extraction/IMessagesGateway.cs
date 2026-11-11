using System.Text.Json;

namespace PrescriptionExtractor.Core.Extraction;

public sealed record GatewayRequest(
    string Model, string SystemPrompt, string UserText, byte[] FileBytes, string MediaType,
    JsonElement Schema, int MaxTokens, string Effort);

public sealed record GatewayResponse(
    string StopReason, string? Text, long InputTokens, long OutputTokens, string? RefusalCategory);

public interface IMessagesGateway
{
    Task<GatewayResponse> SendAsync(GatewayRequest request, CancellationToken ct);
}
