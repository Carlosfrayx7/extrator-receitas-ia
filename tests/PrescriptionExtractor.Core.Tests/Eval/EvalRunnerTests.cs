using FluentAssertions;
using PrescriptionExtractor.Core.Domain;
using PrescriptionExtractor.Core.Extraction;
using PrescriptionExtractor.Core.Validation;
using PrescriptionExtractor.Eval;

namespace PrescriptionExtractor.Core.Tests.Eval;

public class EvalRunnerTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("eval-test").FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private void AddDocument(string name, Prescription expected)
    {
        File.WriteAllBytes(Path.Combine(_dir, name), [1, 2, 3]);
        File.WriteAllText(Path.Combine(_dir, name + ".expected.json"), PrescriptionJson.Serialize(expected));
    }

    private sealed class ScriptedExtractor(Func<int, ExtractionResult> next) : IPrescriptionExtractor
    {
        private int _calls;
        public Task<ExtractionResult> ExtractAsync(byte[] file, string mediaType, string? model, CancellationToken ct) =>
            Task.FromResult(next(Interlocked.Increment(ref _calls)));
    }

    private static ExtractionResult Result(Prescription p, long latency = 1000) =>
        new(p, ExtractionStatus.Valid, [], ClaudeModels.Haiku, 1000, 200, 0.0002m, latency);

    [Fact]
    public async Task Scores_every_document_and_aggregates_metrics()
    {
        var rx = TestData.ValidPrescription();
        AddDocument("rx-01-typed.pdf", rx);
        AddDocument("rx-21-handwritten.png", rx);

        var runner = new EvalRunner(new ScriptedExtractor(_ => Result(rx)));
        var run = await runner.RunAsync(_dir, ClaudeModels.Haiku, CancellationToken.None);

        run.Documents.Should().HaveCount(2);
        run.FieldAccuracy.Should().Be(1.0);
        run.PerfectRate.Should().Be(1.0);
        run.TotalCostUsd.Should().Be(0.0004m);
        run.AccuracyByKind.Keys.Should().BeEquivalentTo(["typed-pdf", "handwritten"]);
    }

    [Fact]
    public async Task Errors_are_recorded_and_do_not_stop_the_run()
    {
        AddDocument("rx-01-typed.pdf", TestData.ValidPrescription());
        AddDocument("rx-02-typed.pdf", TestData.ValidPrescription());

        var runner = new EvalRunner(new ScriptedExtractor(n =>
            n == 1 ? throw new InvalidModelOutputException("x") : Result(TestData.ValidPrescription())), parallelism: 1);
        var run = await runner.RunAsync(_dir, ClaudeModels.Haiku, CancellationToken.None);

        run.ErrorCount.Should().Be(1);
        run.ValidJsonRate.Should().Be(0.5);
        run.Documents.Single(d => d.Error is not null).Error.Should().Contain("InvalidModelOutputException");
    }

    [Fact]
    public void AccuracyByConfidence_ignores_fields_without_model_confidence()
    {
        var fields = new List<FieldScore>
        {
            new("a", true, Confidence.High), new("b", false, Confidence.High),
            new("prescriptionType", false, null), new("item[X].extra", false, null),
        };
        var score = new DocumentScore("d", true, true, fields);
        var run = new ModelRun("m", [new DocumentRun("d", "typed-pdf", score, null, null)]);

        run.AccuracyByConfidence.Keys.Should().BeEquivalentTo([Confidence.High]);
        run.AccuracyByConfidence[Confidence.High].Total.Should().Be(2);
        run.AccuracyByConfidence[Confidence.High].Accuracy.Should().Be(0.5);
    }
}
