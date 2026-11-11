using FluentAssertions;
using PrescriptionExtractor.Core.Extraction;

namespace PrescriptionExtractor.Core.Tests.Extraction;

public class ModelPricingTests
{
    [Fact]
    public void Opus_cost_uses_4_and_20_per_million() =>
        ModelPricing.EstimateUsd(ClaudeModels.Opus, 1_000_000, 100_000).Should().Be(6.00m);

    [Fact]
    public void Haiku_cost_uses_0_10_and_0_50_per_million() =>
        ModelPricing.EstimateUsd(ClaudeModels.Haiku, 2_000_000, 1_000_000).Should().Be(0.70m);

    [Theory]
    [InlineData(null, ClaudeModels.Opus)]
    [InlineData("", ClaudeModels.Opus)]
    [InlineData("opus", ClaudeModels.Opus)]
    [InlineData("HAIKU", ClaudeModels.Haiku)]
    [InlineData("claude-haiku-5-5", ClaudeModels.Haiku)]
    public void Resolve_maps_names(string? input, string expected) =>
        ClaudeModels.Resolve(input).Should().Be(expected);

    [Fact]
    public void Resolve_rejects_unknown_model()
    {
        var act = () => ClaudeModels.Resolve("gpt-x");
        act.Should().Throw<UnsupportedModelException>();
    }
}
