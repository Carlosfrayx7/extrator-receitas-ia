using PrescriptionExtractor.Core.Domain;

namespace PrescriptionExtractor.Core.Tests;

public static class TestData
{
    public static ExtractedField F(string? value, Confidence c = Confidence.High) => new(value, c);

    public static PrescriptionItem Item(string medication = "Amoxicilina") => new(
        F(medication), F("500 mg"), F("cápsula"), F("1 cápsula de 8 em 8 horas"), F("7 dias"), F("21 cápsulas"));

    public static Prescription ValidPrescription(DateOnly? issueDate = null) => new(
        IsPrescription: true,
        PrescriptionType: PrescriptionType.Simples,
        Prescriber: new Prescriber(F("Dra. Helena Prado"), F("123456"), F("SP")),
        PatientName: F("Joao Teste"),
        IssueDate: F((issueDate ?? new DateOnly(2026, 11, 1)).ToString("yyyy-MM-dd")),
        Items: [Item()]);
}
