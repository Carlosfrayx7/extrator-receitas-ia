using Anthropic.Core;
using Anthropic.Models.Messages;
using FluentAssertions;
using PrescriptionExtractor.Core.Extraction;

namespace PrescriptionExtractor.Core.Tests.Extraction;

public class WireValueTests
{
    [Fact]
    public void Maps_stop_reasons_to_wire_strings()
    {
        WireValue.Of<StopReason>(StopReason.EndTurn).Should().Be("end_turn");
        WireValue.Of<StopReason>(StopReason.MaxTokens).Should().Be("max_tokens");
        WireValue.Of<StopReason>(StopReason.Refusal).Should().Be("refusal");
    }

    [Fact]
    public void Null_maps_to_null()
    {
        WireValue.Of<StopReason>(null).Should().BeNull();
    }
}
