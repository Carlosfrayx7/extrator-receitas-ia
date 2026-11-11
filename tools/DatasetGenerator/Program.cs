using DatasetGenerator;
using PrescriptionExtractor.Core.Domain;
using QuestPDF.Infrastructure;
using SkiaSharp;

QuestPDF.Settings.License = LicenseType.Community;

var output = ArgValue("--output") ?? Path.Combine(FindRepoRoot(), "dataset");
var samples = ArgValue("--samples");
Directory.CreateDirectory(output);

const int seed = 20261111;
var reference = new DateOnly(2026, 11, 11);
var data = new FakeData(seed);
var rng = new Random(seed);
var fontsDir = Path.Combine(AppContext.BaseDirectory, "fonts");
using var handwriting = SKTypeface.FromFile(Path.Combine(fontsDir, "Caveat-Regular.ttf"))
    ?? throw new FileNotFoundException("fonts/Caveat-Regular.ttf não encontrada.");
using var typed = SKTypeface.FromFamilyName("DejaVu Sans") ?? SKTypeface.Default;

for (var n = 1; n <= 30; n++)
{
    var p = data.NextPrescription(reference);
    var (name, bytes) = n switch
    {
        <= 10 => ($"rx-{n:00}-typed.pdf", PrescriptionRenderer.Pdf(p)),
        <= 20 => ($"rx-{n:00}-typed.png", PrescriptionRenderer.Png(p, typed, handwritten: false, rng)),
        _ => ($"rx-{n:00}-handwritten.png", PrescriptionRenderer.Png(p, handwriting, handwritten: true, rng)),
    };
    Write(name, bytes, p);
}

foreach (var (name, lines) in NonPrescriptionRenderer.Documents)
    Write($"{name}.png", NonPrescriptionRenderer.Png(lines, typed), FakeData.NotAPrescription());

if (samples is not null)
{
    Directory.CreateDirectory(samples);
    foreach (var name in new[] { "rx-01-typed.pdf", "rx-21-handwritten.png", "other-33.png" })
        File.Copy(Path.Combine(output, name), Path.Combine(samples, name), overwrite: true);
}

Console.WriteLine($"Base gerada em {output}");

void Write(string name, byte[] bytes, Prescription expected)
{
    File.WriteAllBytes(Path.Combine(output, name), bytes);
    File.WriteAllText(Path.Combine(output, name + ".expected.json"), PrescriptionJson.Serialize(expected));
}

string? ArgValue(string flag)
{
    var i = Array.IndexOf(args, flag);
    return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
}

static string FindRepoRoot()
{
    var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "ExtratorReceitas.sln"))) dir = dir.Parent;
    return dir?.FullName ?? Directory.GetCurrentDirectory();
}
