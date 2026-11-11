using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using PrescriptionExtractor.Core.Domain;
using PrescriptionExtractor.Core.Extraction;

namespace PrescriptionExtractor.Eval;

public static class ReportWriter
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public static string Write(IReadOnlyList<ModelRun> runs, DateTimeOffset when)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Avaliação: Claude Opus 5.5 × Claude Haiku 5.5");
        sb.AppendLine();
        sb.AppendLine($"Executada em {when.ToString("dd/MM/yyyy", PtBr)} sobre {runs[0].Documents.Count} documentos fictícios " +
                      "(10 PDF digitados, 10 PNG digitados, 10 PNG manuscritos, 3 que não são receita). Esforço `medium`.");
        sb.AppendLine();

        sb.Append("| Métrica |");
        foreach (var r in runs) sb.Append($" {Label(r.Model)} |");
        sb.AppendLine();
        sb.Append("|---|");
        foreach (var _ in runs) sb.Append("---|");
        sb.AppendLine();

        Row(sb, runs, "Precisão por campo", r => Pct(r.FieldAccuracy));
        foreach (var kind in new[] { "typed-pdf", "typed-png", "handwritten" })
            Row(sb, runs, $"  · {KindLabel(kind)}", r => Pct(r.AccuracyByKind.GetValueOrDefault(kind)));
        Row(sb, runs, "Documentos 100% corretos", r => Pct(r.PerfectRate));
        Row(sb, runs, "Reconhece o que não é receita", r => Pct(Recognition(r)));
        Row(sb, runs, "JSON válido", r => Pct(r.ValidJsonRate));
        Row(sb, runs, "Erros da API", r => r.ErrorCount.ToString(PtBr));
        Row(sb, runs, "Acerto quando confiança alta", r => Pct(Calibration(r, Confidence.High)));
        Row(sb, runs, "Acerto quando confiança baixa", r => Pct(Calibration(r, Confidence.Low)));
        Row(sb, runs, "Latência média", r => $"{(r.AvgLatencyMs / 1000).ToString("0.0", PtBr)} s");
        Row(sb, runs, "Latência p95", r => $"{(r.P95LatencyMs / 1000).ToString("0.0", PtBr)} s");
        Row(sb, runs, "Tokens (entrada / saída)", r => $"{r.TotalInputTokens.ToString("N0", PtBr)} / {r.TotalOutputTokens.ToString("N0", PtBr)}");
        Row(sb, runs, "Custo total", r => $"US$ {r.TotalCostUsd.ToString("0.0000", PtBr)}");

        sb.AppendLine();
        sb.AppendLine("## Campos que mais erraram");
        foreach (var r in runs)
        {
            sb.AppendLine();
            sb.AppendLine($"**{Label(r.Model)}**");
            var worst = r.Documents.Where(d => d.Score is not null).SelectMany(d => d.Score!.Fields)
                .Where(f => !f.Correct)
                .GroupBy(f => Regex.Replace(f.Field, @"\[[^\]]*\]", "[]"))
                .OrderByDescending(g => g.Count()).ThenBy(g => g.Key, StringComparer.Ordinal).Take(5).ToList();
            if (worst.Count == 0) sb.AppendLine("- nenhum erro");
            foreach (var g in worst) sb.AppendLine($"- `{g.Key}`: {g.Count()} erro(s)");
        }

        sb.AppendLine();
        sb.AppendLine("## Conclusão");
        sb.AppendLine();
        sb.AppendLine(Conclusion(runs));
        return sb.ToString();
    }

    private static string Conclusion(IReadOnlyList<ModelRun> runs)
    {
        var opus = runs.FirstOrDefault(r => r.Model == ClaudeModels.Opus);
        var haiku = runs.FirstOrDefault(r => r.Model == ClaudeModels.Haiku);
        if (opus is null || haiku is null || opus.TotalCostUsd == 0) return "Rodar os dois modelos para comparar.";
        var costShare = (double)(haiku.TotalCostUsd / opus.TotalCostUsd);
        var gap = (opus.FieldAccuracy - haiku.FieldAccuracy) * 100;
        return $"O Haiku 5.5 custou {costShare.ToString("0.0%", PtBr).Replace(" ", "")} do Opus 5.5, " +
               $"com {gap.ToString("0.0", PtBr)} pontos percentuais de diferença na precisão por campo.";
    }

    private static double Recognition(ModelRun r)
    {
        var others = r.Documents.Where(d => d.Kind == "other").ToList();
        return others.Count == 0 ? 0 : (double)others.Count(d => d.Score?.RecognizedCorrectly == true) / others.Count;
    }

    private static double Calibration(ModelRun r, Confidence c) =>
        r.AccuracyByConfidence.TryGetValue(c, out var v) ? v.Accuracy : 0;

    private static void Row(StringBuilder sb, IReadOnlyList<ModelRun> runs, string label, Func<ModelRun, string> value)
    {
        sb.Append($"| {label} |");
        foreach (var r in runs) sb.Append($" {value(r)} |");
        sb.AppendLine();
    }

    private static string Pct(double v) => v.ToString("0.0%", PtBr).Replace(" ", "");
    private static string Label(string model) => model == ClaudeModels.Haiku ? "Haiku 5.5" : "Opus 5.5";
    private static string KindLabel(string kind) => kind switch
    {
        "typed-pdf" => "PDF digitado", "typed-png" => "PNG digitado", _ => "PNG manuscrito",
    };
}
