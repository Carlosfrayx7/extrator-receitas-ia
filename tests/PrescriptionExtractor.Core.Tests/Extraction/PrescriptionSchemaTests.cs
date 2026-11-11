using System.Text.Json;
using FluentAssertions;
using PrescriptionExtractor.Core.Extraction;

namespace PrescriptionExtractor.Core.Tests.Extraction;

public class PrescriptionSchemaTests
{
    [Fact]
    public void Every_object_is_strict_and_requires_all_properties()
    {
        foreach (var obj in Objects(PrescriptionSchema.Schema))
        {
            obj.GetProperty("additionalProperties").GetBoolean().Should().BeFalse();
            var properties = obj.GetProperty("properties").EnumerateObject().Select(p => p.Name).Order();
            var required = obj.GetProperty("required").EnumerateArray().Select(e => e.GetString()!).Order();
            required.Should().Equal(properties);
        }
    }

    [Fact]
    public void Field_value_is_nullable_and_confidence_is_an_enum()
    {
        var field = PrescriptionSchema.Schema.GetProperty("properties").GetProperty("patientName");
        field.GetProperty("properties").GetProperty("value").GetProperty("type").EnumerateArray()
            .Select(e => e.GetString()).Should().BeEquivalentTo(["string", "null"]);
        field.GetProperty("properties").GetProperty("confidence").GetProperty("enum").EnumerateArray()
            .Select(e => e.GetString()).Should().BeEquivalentTo(["high", "medium", "low"]);
    }

    [Fact]
    public void Prompt_asks_for_literal_extraction()
    {
        PrescriptionSchema.SystemPrompt.Should().Contain("literal").And.Contain("null");
    }

    private static IEnumerable<JsonElement> Objects(JsonElement e)
    {
        if (e.ValueKind != JsonValueKind.Object) yield break;
        if (e.TryGetProperty("type", out var t) && t.ValueKind == JsonValueKind.String && t.GetString() == "object")
            yield return e;
        foreach (var p in e.EnumerateObject())
        {
            if (p.Value.ValueKind == JsonValueKind.Object)
                foreach (var o in Objects(p.Value)) yield return o;
        }
    }
}
