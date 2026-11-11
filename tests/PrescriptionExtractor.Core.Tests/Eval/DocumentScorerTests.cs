using FluentAssertions;
using PrescriptionExtractor.Core.Domain;
using PrescriptionExtractor.Eval;
using static PrescriptionExtractor.Core.Tests.TestData;

namespace PrescriptionExtractor.Core.Tests.Eval;

public class DocumentScorerTests
{
    [Fact]
    public void Identical_prescription_is_perfect()
    {
        var p = ValidPrescription();
        var score = DocumentScorer.Score("rx-01", p, p);
        score.Perfect.Should().BeTrue();
        score.Total.Should().Be(5 + 6 + 1); // 5 campos do cabeçalho + 6 do item + tipo
        score.RecognizedCorrectly.Should().BeTrue();
    }

    [Fact]
    public void Items_are_matched_by_medication_regardless_of_order()
    {
        var expected = ValidPrescription() with { Items = [Item("Amoxicilina"), Item("Dipirona")] };
        var actual = expected with { Items = [Item("Dipirona"), Item("Amoxicilina")] };
        DocumentScorer.Score("rx", expected, actual).Perfect.Should().BeTrue();
    }

    [Fact]
    public void Missing_item_counts_all_its_fields_as_wrong()
    {
        var expected = ValidPrescription() with { Items = [Item("Amoxicilina"), Item("Dipirona")] };
        var actual = expected with { Items = [Item("Amoxicilina")] };
        var score = DocumentScorer.Score("rx", expected, actual);
        score.Fields.Count(f => !f.Correct).Should().Be(6);
    }

    [Fact]
    public void Wrong_field_keeps_the_model_confidence_for_calibration()
    {
        var expected = ValidPrescription();
        var actual = expected with { PatientName = new ExtractedField("Outro Nome", Confidence.Low) };
        var field = DocumentScorer.Score("rx", expected, actual).Fields.Single(f => f.Field == "patientName");
        field.Correct.Should().BeFalse();
        field.ActualConfidence.Should().Be(Confidence.Low);
    }

    [Fact]
    public void Non_prescription_scores_only_recognition()
    {
        var expected = new Prescription(false, PrescriptionType.Desconhecido,
            new Prescriber(ExtractedField.Missing, ExtractedField.Missing, ExtractedField.Missing),
            ExtractedField.Missing, ExtractedField.Missing, []);
        var score = DocumentScorer.Score("other", expected, ValidPrescription());
        score.RecognizedCorrectly.Should().BeFalse();
        score.Fields.Should().BeEmpty();
    }
}
