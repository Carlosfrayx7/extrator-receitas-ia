namespace PrescriptionExtractor.Core.Validation;

public enum Severity { Error, Warning }

public enum ExtractionStatus { Valid, NeedsReview, Invalid }

public sealed record ValidationIssue(string Code, string Field, string Message, Severity Severity);

public sealed record ValidationReport(ExtractionStatus Status, IReadOnlyList<ValidationIssue> Issues);
