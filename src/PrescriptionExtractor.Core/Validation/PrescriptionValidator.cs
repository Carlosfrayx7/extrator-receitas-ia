using System.Globalization;
using System.Text.RegularExpressions;
using PrescriptionExtractor.Core.Domain;

namespace PrescriptionExtractor.Core.Validation;

public sealed partial class PrescriptionValidator(TimeProvider clock)
{
    private const int ExpirationDays = 30;

    private static readonly HashSet<string> States =
    [
        "AC", "AL", "AP", "AM", "BA", "CE", "DF", "ES", "GO", "MA", "MT", "MS", "MG", "PA",
        "PB", "PR", "PE", "PI", "RJ", "RN", "RS", "RO", "RR", "SC", "SP", "SE", "TO",
    ];

    [GeneratedRegex(@"^\d{4,7}$")]
    private static partial Regex CrmPattern();

    [GeneratedRegex(@"\d\s*(mg|g|mcg|µg|ml|ui|%)(\b|$)", RegexOptions.IgnoreCase)]
    private static partial Regex StrengthUnitPattern();

    public ValidationReport Validate(Prescription p)
    {
        var issues = new List<ValidationIssue>();

        if (!p.IsPrescription)
        {
            issues.Add(Error("not_a_prescription", "isPrescription", "O documento não é uma receita médica."));
            return Report(issues);
        }

        if (p.Items.Count == 0)
            issues.Add(Error("no_items", "items", "Nenhum medicamento encontrado na receita."));

        CheckCrm(p.Prescriber, issues);
        CheckDate(p.IssueDate, issues);

        for (var i = 0; i < p.Items.Count; i++)
            CheckItem(p.Items[i], $"items[{i}]", p.PrescriptionType, issues);

        foreach (var (field, value) in AllFields(p))
            if (value.Confidence == Confidence.Low && value.HasValue)
                issues.Add(Warning("low_confidence", field, "Leitura com confiança baixa: revisar manualmente."));

        return Report(issues);
    }

    private static void CheckCrm(Prescriber prescriber, List<ValidationIssue> issues)
    {
        if (prescriber.CrmNumber.HasValue && !CrmPattern().IsMatch(prescriber.CrmNumber.Value!.Trim()))
            issues.Add(Warning("invalid_crm", "prescriber.crmNumber", "CRM deve ter de 4 a 7 dígitos."));

        if (prescriber.CrmState.HasValue && !States.Contains(prescriber.CrmState.Value!.Trim().ToUpperInvariant()))
            issues.Add(Warning("invalid_state", "prescriber.crmState", "UF do CRM inexistente."));
    }

    private void CheckDate(ExtractedField issueDate, List<ValidationIssue> issues)
    {
        if (!issueDate.HasValue) return;

        if (!DateOnly.TryParseExact(issueDate.Value!.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var date))
        {
            issues.Add(Warning("invalid_date", "issueDate", "Data da receita inválida."));
            return;
        }

        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        if (date > today)
            issues.Add(Warning("future_date", "issueDate", "Data da receita no futuro."));
        else if (today.DayNumber - date.DayNumber > ExpirationDays)
            issues.Add(Warning("possibly_expired", "issueDate", $"Receita emitida há mais de {ExpirationDays} dias."));
    }

    private static void CheckItem(PrescriptionItem item, string path, PrescriptionType type, List<ValidationIssue> issues)
    {
        if (!item.Medication.HasValue)
            issues.Add(Error("missing_medication", $"{path}.medication", "Medicamento não identificado."));

        if (!item.Dosage.HasValue)
            issues.Add(Error("missing_dosage", $"{path}.dosage", "Posologia não identificada."));

        if (item.Strength.HasValue && !StrengthUnitPattern().IsMatch(item.Strength.Value!))
            issues.Add(Warning("strength_without_unit", $"{path}.strength", "Concentração sem unidade reconhecida."));

        if (type == PrescriptionType.Antimicrobiano && !item.Duration.HasValue)
            issues.Add(Warning("antimicrobial_without_duration", $"{path}.duration",
                "Antimicrobiano sem duração do tratamento."));
    }

    private static IEnumerable<(string Field, ExtractedField Value)> AllFields(Prescription p)
    {
        yield return ("prescriber.name", p.Prescriber.Name);
        yield return ("prescriber.crmNumber", p.Prescriber.CrmNumber);
        yield return ("prescriber.crmState", p.Prescriber.CrmState);
        yield return ("patientName", p.PatientName);
        yield return ("issueDate", p.IssueDate);
        for (var i = 0; i < p.Items.Count; i++)
        {
            var it = p.Items[i];
            yield return ($"items[{i}].medication", it.Medication);
            yield return ($"items[{i}].strength", it.Strength);
            yield return ($"items[{i}].form", it.Form);
            yield return ($"items[{i}].dosage", it.Dosage);
            yield return ($"items[{i}].duration", it.Duration);
            yield return ($"items[{i}].quantity", it.Quantity);
        }
    }

    private static ValidationReport Report(List<ValidationIssue> issues)
    {
        var status = issues.Any(i => i.Severity == Severity.Error) ? ExtractionStatus.Invalid
            : issues.Count > 0 ? ExtractionStatus.NeedsReview
            : ExtractionStatus.Valid;
        return new ValidationReport(status, issues);
    }

    private static ValidationIssue Error(string code, string field, string message) => new(code, field, message, Severity.Error);
    private static ValidationIssue Warning(string code, string field, string message) => new(code, field, message, Severity.Warning);
}
