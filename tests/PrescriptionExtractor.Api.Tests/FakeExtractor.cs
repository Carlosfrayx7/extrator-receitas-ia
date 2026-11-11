using PrescriptionExtractor.Core.Domain;
using PrescriptionExtractor.Core.Extraction;
using PrescriptionExtractor.Core.Validation;

namespace PrescriptionExtractor.Api.Tests;

public sealed class FakeExtractor : IPrescriptionExtractor
{
    public Func<ExtractionResult>? Behavior { get; set; }
    public string? LastModel { get; private set; }

    public Task<ExtractionResult> ExtractAsync(byte[] file, string mediaType, string? model, CancellationToken ct)
    {
        FileRules.EnsureValid(file, mediaType);
        LastModel = model;
        return Task.FromResult((Behavior ?? Ok)());
    }

    public static ExtractionResult Ok() => new(
        new Prescription(true, PrescriptionType.Simples,
            new Prescriber(new("Dra. X", Confidence.High), new("1234", Confidence.High), new("SP", Confidence.High)),
            new("Paciente Teste", Confidence.High), new("2026-11-10", Confidence.High),
            [new PrescriptionItem(new("Dipirona", Confidence.High), new("500 mg", Confidence.High),
                new("comprimido", Confidence.High), new("1 comprimido de 6 em 6 horas", Confidence.High),
                new("3 dias", Confidence.High), new("12 comprimidos", Confidence.High))]),
        ExtractionStatus.Valid, [], ClaudeModels.Opus, 1500, 400, 0.014m, 2100);
}
