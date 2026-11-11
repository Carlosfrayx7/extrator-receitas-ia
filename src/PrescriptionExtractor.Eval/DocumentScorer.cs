using PrescriptionExtractor.Core.Domain;

namespace PrescriptionExtractor.Eval;

public sealed record FieldScore(string Field, bool Correct, Confidence ActualConfidence);

public sealed record DocumentScore(string Document, bool ExpectedPrescription, bool RecognizedCorrectly,
    IReadOnlyList<FieldScore> Fields)
{
    public int Correct => Fields.Count(f => f.Correct);
    public int Total => Fields.Count;
    public bool Perfect => RecognizedCorrectly && Fields.All(f => f.Correct);
}

public static class DocumentScorer
{
    public static DocumentScore Score(string document, Prescription expected, Prescription actual)
    {
        var recognized = expected.IsPrescription == actual.IsPrescription;
        if (!expected.IsPrescription || !actual.IsPrescription)
            return new DocumentScore(document, expected.IsPrescription, recognized,
                expected.IsPrescription ? AllWrong(expected) : []);

        var fields = new List<FieldScore>
        {
            Compare("prescriber.name", expected.Prescriber.Name, actual.Prescriber.Name),
            Compare("prescriber.crmNumber", expected.Prescriber.CrmNumber, actual.Prescriber.CrmNumber),
            Compare("prescriber.crmState", expected.Prescriber.CrmState, actual.Prescriber.CrmState),
            Compare("patientName", expected.PatientName, actual.PatientName),
            Compare("issueDate", expected.IssueDate, actual.IssueDate),
            new("prescriptionType", expected.PrescriptionType == actual.PrescriptionType, Confidence.High),
        };

        var remaining = actual.Items.ToList();
        foreach (var exp in expected.Items)
        {
            var match = remaining.FirstOrDefault(a => FieldNormalizer.Equivalent(exp.Medication.Value, a.Medication.Value));
            if (match is not null) remaining.Remove(match);
            fields.AddRange(CompareItem(exp, match));
        }

        return new DocumentScore(document, true, recognized, fields);
    }

    private static IEnumerable<FieldScore> CompareItem(PrescriptionItem exp, PrescriptionItem? act)
    {
        var name = exp.Medication.Value;
        yield return Compare($"item[{name}].medication", exp.Medication, act?.Medication);
        yield return Compare($"item[{name}].strength", exp.Strength, act?.Strength);
        yield return Compare($"item[{name}].form", exp.Form, act?.Form);
        yield return Compare($"item[{name}].dosage", exp.Dosage, act?.Dosage);
        yield return Compare($"item[{name}].duration", exp.Duration, act?.Duration);
        yield return Compare($"item[{name}].quantity", exp.Quantity, act?.Quantity);
    }

    private static FieldScore Compare(string field, ExtractedField expected, ExtractedField? actual) =>
        new(field, actual is not null && FieldNormalizer.Equivalent(expected.Value, actual.Value),
            actual?.Confidence ?? Confidence.Low);

    private static List<FieldScore> AllWrong(Prescription expected) =>
        Score("x", expected, expected).Fields.Select(f => f with { Correct = false, ActualConfidence = Confidence.Low }).ToList();
}
