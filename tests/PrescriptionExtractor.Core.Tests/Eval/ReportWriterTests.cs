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

    private static readonly DateTimeOffset When = new(2026, 11, 11, 0, 0, 0, TimeSpan.Zero);

    private static ModelRun RunWithFields(string model, params FieldScore[] fields) =>
        new(model, [new DocumentRun("d.pdf", "typed-pdf", new DocumentScore("d.pdf", true, true, fields), null, null)]);

    private static ModelRun ErroredRun(string model) =>
        new(model, [new DocumentRun("d.pdf", "typed-pdf", null, null, "X: boom")]);

    [Fact]
    public void Prints_nd_when_there_is_no_data_and_count_when_there_is()
    {
        var md = ReportWriter.Write([RunWithFields(ClaudeModels.Haiku,
            new FieldScore("a", true, Confidence.High), new FieldScore("b", false, Confidence.High))], When);

        md.Should().Contain("| Acerto quando confiança baixa | n/d |")
            .And.Contain("| Acerto quando confiança alta | 50,0% (n=2) |")
            .And.Contain("| Reconhece o que não é receita | n/d |")
            .And.Contain("|   · PNG digitado | n/d |");
    }

    [Fact]
    public void Conclusion_when_a_model_is_missing()
    {
        ReportWriter.Write([Run(ClaudeModels.Haiku, 0.001m)], When).Should().Contain("Rodar os dois modelos para comparar.");
    }

    [Fact]
    public void Conclusion_when_a_model_had_no_successful_documents()
    {
        var md = ReportWriter.Write([ErroredRun(ClaudeModels.Opus), Run(ClaudeModels.Haiku, 0.001m)], When);

        md.Should().Contain("Sem resultados válidos de Opus 5.5 para comparar (todas as chamadas falharam).");
    }

    [Fact]
    public void Conclusion_phrases_higher_haiku_accuracy_without_negative_number()
    {
        var opus = RunWithFields(ClaudeModels.Opus, new FieldScore("a", false, Confidence.High), new FieldScore("b", true, Confidence.High));
        var haiku = RunWithFields(ClaudeModels.Haiku, new FieldScore("a", true, Confidence.High), new FieldScore("b", true, Confidence.High));
        var withCost = new[]
        {
            new ModelRun(opus.Model, [opus.Documents[0] with { Result = new ExtractionResult(TestData.ValidPrescription(), ExtractionStatus.Valid, [], opus.Model, 1, 1, 0.02m, 1) }]),
            new ModelRun(haiku.Model, [haiku.Documents[0] with { Result = new ExtractionResult(TestData.ValidPrescription(), ExtractionStatus.Valid, [], haiku.Model, 1, 1, 0.001m, 1) }]),
        };

        var md = ReportWriter.Write(withCost, When);

        md.Should().Contain("50,0 pontos percentuais a mais de precisão por campo").And.NotContain("-50");
    }
}
