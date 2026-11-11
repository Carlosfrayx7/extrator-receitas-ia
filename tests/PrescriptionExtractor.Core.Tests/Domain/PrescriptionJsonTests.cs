using System.Text.Json;
using FluentAssertions;
using PrescriptionExtractor.Core.Domain;

namespace PrescriptionExtractor.Core.Tests.Domain;

public class PrescriptionJsonTests
{
    private const string ValidJson = """
    {
      "isPrescription": true,
      "prescriptionType": "antimicrobiano",
      "prescriber": {
        "name": { "value": "Dra. Helena Prado", "confidence": "high" },
        "crmNumber": { "value": "123456", "confidence": "high" },
        "crmState": { "value": "SP", "confidence": "medium" }
      },
      "patientName": { "value": "Joao Teste", "confidence": "high" },
      "issueDate": { "value": "2026-11-01", "confidence": "high" },
      "items": [
        {
          "medication": { "value": "Amoxicilina", "confidence": "high" },
          "strength": { "value": "500 mg", "confidence": "high" },
          "form": { "value": "cápsula", "confidence": "high" },
          "dosage": { "value": "1 cápsula de 8 em 8 horas", "confidence": "medium" },
          "duration": { "value": "7 dias", "confidence": "high" },
          "quantity": { "value": null, "confidence": "low" }
        }
      ]
    }
    """;

    [Fact]
    public void Parse_reads_all_fields_and_enums()
    {
        var p = PrescriptionJson.Parse(ValidJson);

        p.IsPrescription.Should().BeTrue();
        p.PrescriptionType.Should().Be(PrescriptionType.Antimicrobiano);
        p.Prescriber.CrmState.Should().Be(new ExtractedField("SP", Confidence.Medium));
        p.Items.Should().ContainSingle();
        p.Items[0].Quantity.Should().Be(new ExtractedField(null, Confidence.Low));
    }

    [Fact]
    public void Serialize_round_trips_with_snake_case_enums()
    {
        var p = PrescriptionJson.Parse(ValidJson) with { PrescriptionType = PrescriptionType.ControleEspecial };

        var json = PrescriptionJson.Serialize(p);

        json.Should().Contain("\"controle_especial\"").And.Contain("\"isPrescription\"");
        PrescriptionJson.Parse(json).Should().BeEquivalentTo(p);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("{\"isPrescription\": true}")]
    public void Parse_rejects_invalid_or_incomplete_json(string json)
    {
        var act = () => PrescriptionJson.Parse(json);
        act.Should().Throw<JsonException>();
    }
}
