using System.Text;
using FluentAssertions;
using PrescriptionExtractor.Core.Extraction;

namespace PrescriptionExtractor.Core.Tests.Extraction;

public class FileRulesTests
{
    [Theory]
    [InlineData("a.png", null, "image/png")]
    [InlineData("a.JPG", null, "image/jpeg")]
    [InlineData("a.jpeg", "image/jpeg", "image/jpeg")]
    [InlineData("a.pdf", "application/pdf", "application/pdf")]
    [InlineData("sem-extensao", "image/png", "image/png")]
    public void Resolves_supported_types(string name, string? contentType, string expected) =>
        FileRules.ResolveMediaType(name, contentType).Should().Be(expected);

    [Theory]
    [InlineData("a.gif", "image/gif")]
    [InlineData("a.docx", null)]
    public void Rejects_unsupported_types(string name, string? contentType)
    {
        var act = () => FileRules.ResolveMediaType(name, contentType);
        act.Should().Throw<UnsupportedFileException>();
    }

    [Fact]
    public void Rejects_empty_and_too_large_files()
    {
        ((Action)(() => FileRules.EnsureValid([], "image/png"))).Should().Throw<UnsupportedFileException>();
        ((Action)(() => FileRules.EnsureValid(new byte[FileRules.MaxBytes + 1], "image/png")))
            .Should().Throw<UnsupportedFileException>();
    }

    [Fact]
    public void Rejects_pdf_with_more_than_five_pages()
    {
        var pages = string.Concat(Enumerable.Repeat("<< /Type /Page /Parent 1 0 R >>\n", 6));
        var pdf = Encoding.ASCII.GetBytes("%PDF-1.7\n<< /Type /Pages /Count 6 >>\n" + pages);
        var act = () => FileRules.EnsureValid(pdf, "application/pdf");
        act.Should().Throw<UnsupportedFileException>().WithMessage("*5 páginas*");
    }
}
