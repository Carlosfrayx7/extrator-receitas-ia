using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using PrescriptionExtractor.Core.Domain;
using PrescriptionExtractor.Core.Validation;
using static PrescriptionExtractor.Core.Tests.TestData;

namespace PrescriptionExtractor.Core.Tests.Validation;

public class PrescriptionValidatorTests
{
    private readonly PrescriptionValidator _validator =
        new(new FakeTimeProvider(new DateTimeOffset(2026, 11, 11, 12, 0, 0, TimeSpan.Zero)));

    private ValidationReport Validate(Prescription p) => _validator.Validate(p);

    [Fact]
    public void Valid_prescription_has_no_issues()
    {
        var report = Validate(ValidPrescription());
        report.Status.Should().Be(ExtractionStatus.Valid);
        report.Issues.Should().BeEmpty();
    }

    [Fact]
    public void Not_a_prescription_is_invalid()
    {
        var report = Validate(ValidPrescription() with { IsPrescription = false });
        report.Status.Should().Be(ExtractionStatus.Invalid);
        report.Issues.Should().ContainSingle(i => i.Code == "not_a_prescription" && i.Severity == Severity.Error);
    }

    [Fact]
    public void No_items_is_invalid()
    {
        var report = Validate(ValidPrescription() with { Items = [] });
        report.Status.Should().Be(ExtractionStatus.Invalid);
        report.Issues.Should().Contain(i => i.Code == "no_items");
    }

    [Fact]
    public void Item_without_medication_or_dosage_is_invalid()
    {
        var item = Item() with { Medication = ExtractedField.Missing, Dosage = F(" ") };
        var report = Validate(ValidPrescription() with { Items = [item] });
        report.Status.Should().Be(ExtractionStatus.Invalid);
        report.Issues.Select(i => i.Code).Should().Contain(["missing_medication", "missing_dosage"]);
        report.Issues.Should().Contain(i => i.Field == "items[0].medication");
    }

    [Theory]
    [InlineData("123")]
    [InlineData("12345678")]
    [InlineData("12A456")]
    public void Crm_outside_4_to_7_digits_is_warning(string crm)
    {
        var p = ValidPrescription() with { Prescriber = new Prescriber(F("Dra. X"), F(crm), F("SP")) };
        var report = Validate(p);
        report.Status.Should().Be(ExtractionStatus.NeedsReview);
        report.Issues.Should().ContainSingle(i => i.Code == "invalid_crm" && i.Severity == Severity.Warning);
    }

    [Fact]
    public void Unknown_state_is_warning()
    {
        var p = ValidPrescription() with { Prescriber = new Prescriber(F("Dra. X"), F("1234"), F("XX")) };
        Validate(p).Issues.Should().ContainSingle(i => i.Code == "invalid_state");
    }

    [Theory]
    [InlineData("2026-13-01", "invalid_date")]
    [InlineData("01/11/2026", "invalid_date")]
    [InlineData("2026-11-20", "future_date")]
    [InlineData("2026-09-01", "possibly_expired")]
    public void Date_rules_are_warnings(string date, string code)
    {
        var p = ValidPrescription() with { IssueDate = F(date) };
        Validate(p).Issues.Should().ContainSingle(i => i.Code == code && i.Severity == Severity.Warning);
    }

    [Theory]
    [InlineData("500")]
    [InlineData("quinhentos")]
    public void Strength_without_unit_is_warning(string strength)
    {
        var p = ValidPrescription() with { Items = [Item() with { Strength = F(strength) }] };
        Validate(p).Issues.Should().ContainSingle(i => i.Code == "strength_without_unit");
    }

    [Theory]
    [InlineData("500 mg")]
    [InlineData("500mg")]
    [InlineData("0,5 g")]
    [InlineData("250 mcg")]
    [InlineData("5 mL")]
    [InlineData("100.000 UI")]
    [InlineData("1%")]
    public void Strength_with_known_unit_is_accepted(string strength)
    {
        var p = ValidPrescription() with { Items = [Item() with { Strength = F(strength) }] };
        Validate(p).Issues.Should().BeEmpty();
    }

    [Fact]
    public void Missing_strength_is_not_a_unit_warning()
    {
        var p = ValidPrescription() with { Items = [Item() with { Strength = new ExtractedField(null, Confidence.High) }] };
        Validate(p).Issues.Should().NotContain(i => i.Code == "strength_without_unit");
    }

    [Fact]
    public void Antimicrobial_without_duration_is_warning()
    {
        var p = ValidPrescription() with
        {
            PrescriptionType = PrescriptionType.Antimicrobiano,
            Items = [Item() with { Duration = new ExtractedField(null, Confidence.High) }],
        };
        Validate(p).Issues.Should().ContainSingle(i => i.Code == "antimicrobial_without_duration");
    }

    [Fact]
    public void Low_confidence_fields_are_flagged_for_review()
    {
        var p = ValidPrescription() with { PatientName = F("Joao Teste", Confidence.Low) };
        var report = Validate(p);
        report.Status.Should().Be(ExtractionStatus.NeedsReview);
        report.Issues.Should().ContainSingle(i => i.Code == "low_confidence" && i.Field == "patientName");
    }

    [Fact]
    public void Not_a_prescription_skips_field_rules()
    {
        var p = new Prescription(false, PrescriptionType.Desconhecido,
            new Prescriber(ExtractedField.Missing, ExtractedField.Missing, ExtractedField.Missing),
            ExtractedField.Missing, ExtractedField.Missing, []);
        Validate(p).Issues.Should().ContainSingle().Which.Code.Should().Be("not_a_prescription");
    }
}
