using PrescriptionExtractor.Core.Domain;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SkiaSharp;

namespace DatasetGenerator;

public static class PrescriptionRenderer
{
    private const int Width = 1240, Height = 1754; // A4 a 150 dpi

    public static byte[] Pdf(Prescription p) => Document.Create(doc => doc.Page(page =>
    {
        page.Size(PageSizes.A4);
        page.Margin(40);
        page.DefaultTextStyle(t => t.FontSize(12));
        page.Header().Column(c =>
        {
            c.Item().Text(p.Prescriber.Name.Value!).FontSize(18).Bold();
            c.Item().Text($"CRM {p.Prescriber.CrmNumber.Value} / {p.Prescriber.CrmState.Value}");
            c.Item().PaddingTop(4).Text(TypeLabel(p.PrescriptionType)).FontColor(Colors.Grey.Darken1);
            c.Item().PaddingVertical(8).LineHorizontal(1);
        });
        page.Content().Column(c =>
        {
            c.Spacing(8);
            c.Item().Text($"Paciente: {p.PatientName.Value}");
            for (var i = 0; i < p.Items.Count; i++)
            {
                var it = p.Items[i];
                c.Item().Text($"{i + 1}. {it.Medication.Value} {it.Strength.Value} ({it.Form.Value}) ---- {it.Quantity.Value}").Bold();
                c.Item().PaddingLeft(16).Text($"{it.Dosage.Value}, por {it.Duration.Value}.");
            }
        });
        page.Footer().Column(c =>
        {
            c.Item().Text($"Data: {DateOnly.Parse(p.IssueDate.Value!):dd/MM/yyyy}");
            c.Item().PaddingTop(24).Text("__________________________  Assinatura e carimbo");
        });
    })).GeneratePdf();

    public static byte[] Png(Prescription p, SKTypeface typeface, bool handwritten, Random rng)
    {
        using var surface = SKSurface.Create(new SKImageInfo(Width, Height));
        var canvas = surface.Canvas;
        canvas.Clear(handwritten ? new SKColor(250, 247, 238) : SKColors.White);
        if (handwritten)
        {
            canvas.Translate(Width / 2f, Height / 2f);
            canvas.RotateDegrees((float)(rng.NextDouble() * 4 - 2));
            canvas.Translate(-Width / 2f, -Height / 2f);
        }

        using var font = new SKFont(typeface, handwritten ? 44 : 30);
        using var ink = new SKPaint { Color = handwritten ? new SKColor(20, 40, 110) : SKColors.Black, IsAntialias = true };
        float y = 110;
        void Line(string text, float x = 90, float gap = 1.6f) { canvas.DrawText(text, x, y, SKTextAlign.Left, font, ink); y += font.Size * gap; }

        Line(p.Prescriber.Name.Value!);
        Line($"CRM {p.Prescriber.CrmNumber.Value} / {p.Prescriber.CrmState.Value}");
        Line(TypeLabel(p.PrescriptionType), gap: 2.2f);
        Line($"Paciente: {p.PatientName.Value}", gap: 2f);
        for (var i = 0; i < p.Items.Count; i++)
        {
            var it = p.Items[i];
            Line($"{i + 1}. {it.Medication.Value} {it.Strength.Value} ({it.Form.Value})");
            Line($"   {it.Quantity.Value}");
            Line($"   {it.Dosage.Value}, por {it.Duration.Value}.", gap: 2f);
        }
        y = Height - 220;
        Line($"Data: {DateOnly.Parse(p.IssueDate.Value!):dd/MM/yyyy}");

        using var image = surface.Snapshot();
        using var bitmap = SKBitmap.FromImage(image);
        if (handwritten) AddNoise(bitmap, rng);
        using var blurred = handwritten ? Blur(bitmap) : SKImage.FromBitmap(bitmap);
        return blurred.Encode(SKEncodedImageFormat.Png, 90).ToArray();
    }

    private static void AddNoise(SKBitmap bitmap, Random rng)
    {
        for (var i = 0; i < bitmap.Width * bitmap.Height / 60; i++)
        {
            var x = rng.Next(bitmap.Width);
            var y = rng.Next(bitmap.Height);
            var shade = (byte)rng.Next(150, 230);
            bitmap.SetPixel(x, y, new SKColor(shade, shade, shade));
        }
    }

    private static SKImage Blur(SKBitmap bitmap)
    {
        using var surface = SKSurface.Create(new SKImageInfo(bitmap.Width, bitmap.Height));
        using var paint = new SKPaint { ImageFilter = SKImageFilter.CreateBlur(1.2f, 1.2f) };
        surface.Canvas.DrawBitmap(bitmap, 0, 0, new SKSamplingOptions(), paint);
        return surface.Snapshot();
    }

    public static string TypeLabel(PrescriptionType type) => type switch
    {
        PrescriptionType.ControleEspecial => "RECEITUÁRIO DE CONTROLE ESPECIAL",
        PrescriptionType.Antimicrobiano => "RECEITA DE ANTIMICROBIANO",
        _ => "RECEITUÁRIO",
    };
}
