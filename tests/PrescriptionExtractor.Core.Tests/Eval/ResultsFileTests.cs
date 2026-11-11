using System.Text.Json;
using FluentAssertions;
using PrescriptionExtractor.Core.Domain;
using PrescriptionExtractor.Core.Extraction;
using PrescriptionExtractor.Core.Validation;
using PrescriptionExtractor.Eval;

namespace PrescriptionExtractor.Core.Tests.Eval;

public class ResultsFileTests
{
    [Fact]
    public void Serializes_camelCase_with_calibration_and_without_extracted_values()
    {
        var p = TestData.ValidPrescription();
        var result = new ExtractionResult(p, ExtractionStatus.Valid, [], ClaudeModels.Haiku, 1000, 200, 0.0002m, 1200);
        var ok = new DocumentRun("a.pdf", "typed-pdf", DocumentScorer.Score("a.pdf", p, p), result, null);
        var failed = new DocumentRun("b.pdf", "typed-pdf", null, null, "X: boom");

        var json = ResultsFile.Serialize([new ModelRun(ClaudeModels.Haiku, [ok, failed])]);
        var model = JsonDocument.Parse(json).RootElement[0];

        model.GetProperty("fieldAccuracy").GetDouble().Should().Be(1.0);
        model.GetProperty("calibration").GetProperty("high").GetProperty("total").GetInt32().Should().BeGreaterThan(0);
        model.GetProperty("documents")[1].GetProperty("error").GetString().Should().Be("X: boom");
        json.Should().NotContain(p.PatientName.Value!);
    }
}
