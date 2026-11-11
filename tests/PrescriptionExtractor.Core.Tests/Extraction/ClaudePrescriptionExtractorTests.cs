using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using PrescriptionExtractor.Core.Domain;
using PrescriptionExtractor.Core.Extraction;
using PrescriptionExtractor.Core.Tests.Fakes;
using PrescriptionExtractor.Core.Validation;

namespace PrescriptionExtractor.Core.Tests.Extraction;

public class ClaudePrescriptionExtractorTests
{
    private static readonly FakeTimeProvider Clock = new(new DateTimeOffset(2026, 11, 11, 12, 0, 0, TimeSpan.Zero));
    private static readonly string ValidJson = PrescriptionJson.Serialize(TestData.ValidPrescription());

    private static ClaudePrescriptionExtractor Sut(IMessagesGateway gateway) =>
        new(gateway, new PrescriptionValidator(Clock), Clock);

    [Fact]
    public async Task Sends_file_schema_prompt_and_settings()
    {
        var gateway = FakeMessagesGateway.Returning(ValidJson);
        byte[] png = [1, 2, 3];

        await Sut(gateway).ExtractAsync(png, "image/png", "haiku", CancellationToken.None);

        var req = gateway.Requests.Should().ContainSingle().Subject;
        req.Model.Should().Be(ClaudeModels.Haiku);
        req.MediaType.Should().Be("image/png");
        req.FileBytes.Should().Equal(png);
        req.SystemPrompt.Should().Be(PrescriptionSchema.SystemPrompt);
        req.Schema.GetRawText().Should().Be(PrescriptionSchema.Schema.GetRawText());
        req.MaxTokens.Should().Be(4096);
        req.Effort.Should().Be("medium");
    }

    [Fact]
    public async Task Returns_validated_result_with_cost()
    {
        var result = await Sut(FakeMessagesGateway.Returning(ValidJson, 1_000_000, 100_000))
            .ExtractAsync([1], "application/pdf", null, CancellationToken.None);

        result.Status.Should().Be(ExtractionStatus.Valid);
        result.Model.Should().Be(ClaudeModels.Opus);
        result.Prescription.Prescriber.CrmNumber.Value.Should().Be("123456");
        result.EstimatedCostUsd.Should().Be(6.00m);
        result.InputTokens.Should().Be(1_000_000);
    }

    [Fact]
    public async Task Refusal_raises_ModelRefusalException()
    {
        var gateway = new FakeMessagesGateway(_ => new GatewayResponse("refusal", null, 10, 0, "general_harms"));
        var act = () => Sut(gateway).ExtractAsync([1], "image/png", null, CancellationToken.None);
        (await act.Should().ThrowAsync<ModelRefusalException>()).Which.Category.Should().Be("general_harms");
    }

    [Fact]
    public async Task Max_tokens_raises_OutputTruncatedException()
    {
        var gateway = new FakeMessagesGateway(_ => new GatewayResponse("max_tokens", "{\"isPres", 10, 4096, null));
        var act = () => Sut(gateway).ExtractAsync([1], "image/png", null, CancellationToken.None);
        await act.Should().ThrowAsync<OutputTruncatedException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("{\"isPrescription\": true}")]
    public async Task Missing_or_invalid_json_raises_InvalidModelOutputException(string? text)
    {
        var gateway = new FakeMessagesGateway(_ => new GatewayResponse("end_turn", text, 10, 10, null));
        var act = () => Sut(gateway).ExtractAsync([1], "image/png", null, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidModelOutputException>();
    }

    [Fact]
    public async Task Invalid_file_is_rejected_before_calling_the_model()
    {
        var gateway = FakeMessagesGateway.Returning(ValidJson);
        var act = () => Sut(gateway).ExtractAsync([], "image/png", null, CancellationToken.None);
        await act.Should().ThrowAsync<UnsupportedFileException>();
        gateway.Requests.Should().BeEmpty();
    }
}
