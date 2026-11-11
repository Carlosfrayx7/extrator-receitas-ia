using FluentAssertions;
using PrescriptionExtractor.Core.Domain;
using PrescriptionExtractor.Core.Extraction;
using PrescriptionExtractor.Core.Validation;
using PrescriptionExtractor.Eval;

namespace PrescriptionExtractor.Core.Tests.Eval;

public class ReportWriterTests
{
    private static ModelRun Run(string model, decimal cost)
    {
        var p = TestData.ValidPrescription();
        var result = new ExtractionResult(p, ExtractionStatus.Valid, [], model, 1000, 200, cost, 1200);
        var doc = new DocumentRun("rx-01-typed.pdf", "typed-pdf", DocumentScorer.Score("rx-01-typed.pdf", p, p), result, null);
        return new ModelRun(model, [doc]);
    }

    [Fact]
    public void Writes_comparison_table_and_conclusion()
    {
        var md = ReportWriter.Write([Run(ClaudeModels.Opus, 0.02m), Run(ClaudeModels.Haiku, 0.0005m)],
            new DateTimeOffset(2026, 11, 11, 0, 0, 0, TimeSpan.Zero));

        md.Should().Contain("| Métrica |").And.Contain("Opus 5.5").And.Contain("Haiku 5.5")
            .And.Contain("Precisão por campo").And.Contain("Custo total")
            .And.Contain("## Conclusão").And.Contain("2,5%"); // Haiku custa 2,5% do Opus
    }

    [Fact]
    public void Groups_most_frequent_errors_stripping_item_names_and_keeping_extra()
    {
        var fields = new List<FieldScore>
        {
            new("item[Amoxicilina].strength", false, Confidence.High),
            new("item[Dipirona].strength", false, Confidence.Low),
            new("item[Foo].extra", false, null),
        };
        var score = new DocumentScore("d", true, true, fields);
        var run = new ModelRun(ClaudeModels.Haiku, [new DocumentRun("d", "typed-pdf", score, null, null)]);

        var md = ReportWriter.Write([run], new DateTimeOffset(2026, 11, 11, 0, 0, 0, TimeSpan.Zero));

        md.Should().Contain("`item[].strength`: 2 erro(s)").And.Contain("`item[].extra`: 1 erro(s)");
    }
}
