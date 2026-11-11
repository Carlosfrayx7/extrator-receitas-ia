using FluentAssertions;
using PrescriptionExtractor.Eval;

namespace PrescriptionExtractor.Core.Tests.Eval;

public class FieldNormalizerTests
{
    [Theory]
    [InlineData("500mg", "500 mg")]
    [InlineData("500 MG", "500 mg")]
    [InlineData("Cápsula", "capsula")]
    [InlineData("  1 cápsula  de 8 em 8 horas. ", "1 capsula de 8 em 8 horas")]
    [InlineData("Dra. Helena Prado", "dra helena prado")]
    [InlineData("0,5 g", "0.5 g")]
    public void Normalizes_case_accents_spaces_units_and_punctuation(string input, string expected) =>
        FieldNormalizer.Normalize(input).Should().Be(expected);

    [Fact]
    public void Null_and_blank_are_equivalent()
    {
        FieldNormalizer.Equivalent(null, "  ").Should().BeTrue();
        FieldNormalizer.Equivalent("Dipirona", null).Should().BeFalse();
        FieldNormalizer.Equivalent("Amoxicilina 500mg", "amoxicilina 500 mg").Should().BeTrue();
    }
}
