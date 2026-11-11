namespace PrescriptionExtractor.Core.Domain;

public enum PrescriptionType { Simples, ControleEspecial, Antimicrobiano, Desconhecido }

public sealed record Prescriber(ExtractedField Name, ExtractedField CrmNumber, ExtractedField CrmState);

public sealed record PrescriptionItem(
    ExtractedField Medication,
    ExtractedField Strength,
    ExtractedField Form,
    ExtractedField Dosage,
    ExtractedField Duration,
    ExtractedField Quantity);

public sealed record Prescription(
    bool IsPrescription,
    PrescriptionType PrescriptionType,
    Prescriber Prescriber,
    ExtractedField PatientName,
    ExtractedField IssueDate,
    IReadOnlyList<PrescriptionItem> Items);
