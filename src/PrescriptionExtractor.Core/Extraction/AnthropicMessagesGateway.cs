using System.Text.Json;
using Anthropic;
using Anthropic.Models.Messages;

namespace PrescriptionExtractor.Core.Extraction;

public sealed class AnthropicMessagesGateway(AnthropicClient client) : IMessagesGateway
{
    public async Task<GatewayResponse> SendAsync(GatewayRequest request, CancellationToken ct)
    {
        var data = Convert.ToBase64String(request.FileBytes);
        ContentBlockParam fileBlock = request.MediaType == "application/pdf"
            ? new DocumentBlockParam { Source = new Base64PdfSource { Data = data } }
            : new ImageBlockParam
            {
                Source = new Base64ImageSource
                {
                    Data = data,
                    MediaType = request.MediaType == "image/png" ? MediaType.ImagePng : MediaType.ImageJpeg,
                },
            };

        var schema = request.Schema.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.Clone());

        var response = await client.Messages.Create(new MessageCreateParams
        {
            Model = request.Model,
            MaxTokens = request.MaxTokens,
            System = request.SystemPrompt,
            OutputConfig = new OutputConfig
            {
                Effort = Effort.Medium,
                Format = new JsonOutputFormat { Schema = schema },
            },
            Messages =
            [
                new UserMessageParam
                {
                    Content = new List<ContentBlockParam> { fileBlock, new TextBlockParam { Text = request.UserText } },
                },
            ],
        }, ct);

        var text = string.Concat(response.Content.Select(b => b.Value).OfType<TextBlock>().Select(t => t.Text));
        return new GatewayResponse(
            WireValue.Of(response.StopReason) ?? "unknown",
            string.IsNullOrWhiteSpace(text) ? null : text,
            response.Usage.InputTokens,
            response.Usage.OutputTokens,
            WireValue.Of(response.StopDetails?.Category));
    }
}
