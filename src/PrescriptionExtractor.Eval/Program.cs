using Anthropic;
using PrescriptionExtractor.Core.Extraction;
using PrescriptionExtractor.Core.Validation;
using PrescriptionExtractor.Eval;

var models = (Arg("--models") ?? $"{ClaudeModels.Opus},{ClaudeModels.Haiku}")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    .Select(ClaudeModels.Resolve).Distinct().ToList();
var root = RepoRoot();
var dataset = Arg("--dataset") ?? Path.Combine(root, "dataset");
var count = Directory.GetFiles(dataset).Count(f => !f.EndsWith(".expected.json"));

if (!AnthropicCredentials.Available())
{
    Console.Error.WriteLine("Sem credenciais: rode `ant auth login` ou defina ANTHROPIC_API_KEY.");
    return 1;
}

// Estimativa conservadora por documento: 2.000 tokens de entrada e 600 de saída.
var estimate = models.Sum(m => ModelPricing.EstimateUsd(m, 2_000L * count, 600L * count));
Console.WriteLine($"{count} documentos × {models.Count} modelo(s). Custo estimado: até US$ {estimate:0.00}.");
if (!args.Contains("--yes"))
{
    Console.Write("Continuar? [s/N] ");
    if (!string.Equals(Console.ReadLine()?.Trim(), "s", StringComparison.OrdinalIgnoreCase)) return 0;
}

var extractor = new ClaudePrescriptionExtractor(
    new AnthropicMessagesGateway(new AnthropicClient()), new PrescriptionValidator(TimeProvider.System), TimeProvider.System);
var runner = new EvalRunner(extractor);

var runs = new List<ModelRun>();
foreach (var model in models)
{
    Console.WriteLine($"Rodando {model}...");
    var run = await runner.RunAsync(dataset, model, CancellationToken.None);
    Console.WriteLine($"  precisão {run.FieldAccuracy:P1}, erros {run.ErrorCount}, custo US$ {run.TotalCostUsd:0.0000}");
    runs.Add(run);
}

var now = DateTimeOffset.Now;
Directory.CreateDirectory(Path.Combine(root, "docs", "eval-results"));
await File.WriteAllTextAsync(Path.Combine(root, "docs", "eval-report.md"), ReportWriter.Write(runs, now));
await File.WriteAllTextAsync(Path.Combine(root, "docs", "eval-results", $"{now:yyyy-MM-dd}.json"), ResultsFile.Serialize(runs));
Console.WriteLine("Relatório em docs/eval-report.md");
return 0;

string? Arg(string flag)
{
    var i = Array.IndexOf(args, flag);
    return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
}

static string RepoRoot()
{
    var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "ExtratorReceitas.sln"))) dir = dir.Parent;
    return dir?.FullName ?? Directory.GetCurrentDirectory();
}
