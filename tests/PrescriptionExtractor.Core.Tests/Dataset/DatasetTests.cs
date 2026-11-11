using FluentAssertions;
using PrescriptionExtractor.Core.Domain;

namespace PrescriptionExtractor.Core.Tests.Dataset;

public class DatasetTests
{
    private static readonly string Dir = Path.Combine(RepoRoot(), "dataset");

    [Fact]
    public void Dataset_has_33_documents_each_with_expected_json()
    {
        var docs = Directory.GetFiles(Dir).Where(f => !f.EndsWith(".expected.json")).ToList();
        docs.Should().HaveCount(33);
        foreach (var doc in docs) File.Exists(doc + ".expected.json").Should().BeTrue(doc);
    }

    [Fact]
    public void Expected_files_parse_and_match_document_kind()
    {
        foreach (var file in Directory.GetFiles(Dir, "*.expected.json"))
        {
            var p = PrescriptionJson.Parse(File.ReadAllText(file));
            var isOther = Path.GetFileName(file).StartsWith("other-");
            p.IsPrescription.Should().Be(!isOther, file);
            if (!isOther) p.Items.Should().HaveCountGreaterThanOrEqualTo(1).And.HaveCountLessThanOrEqualTo(3);
        }
    }

    [Fact]
    public void Dataset_covers_all_kinds()
    {
        var names = Directory.GetFiles(Dir).Select(Path.GetFileName).ToList();
        names.Count(n => n!.EndsWith("-typed.pdf")).Should().Be(10);
        names.Count(n => n!.EndsWith("-typed.png")).Should().Be(10);
        names.Count(n => n!.EndsWith("-handwritten.png")).Should().Be(10);
        names.Count(n => n!.StartsWith("other-") && !n.EndsWith(".json")).Should().Be(3);
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "ExtratorReceitas.sln"))) dir = dir.Parent;
        return dir!.FullName;
    }
}
