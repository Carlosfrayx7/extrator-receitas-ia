using SkiaSharp;

namespace DatasetGenerator;

public static class NonPrescriptionRenderer
{
    public static readonly (string Name, string[] Lines)[] Documents =
    [
        ("other-31", ["NOTA FISCAL (FICTÍCIA)", "Mercado Exemplo LTDA", "CNPJ 00.000.000/0001-00",
            "Arroz 5 kg ....... R$ 24,90", "Feijão 1 kg ...... R$ 8,50", "Total ............ R$ 33,40"]),
        ("other-32", ["CARDÁPIO DO DIA", "Entrada: salada verde", "Prato: frango grelhado com arroz",
            "Sobremesa: pudim", "Bebida: suco natural"]),
        ("other-33", ["ATESTADO (FICTÍCIO)", "Atesto, para os devidos fins, que o paciente",
            "Paciente Ficticio esteve em consulta nesta data", "e deve se afastar por 2 dias.", "Dr. Exemplo - CRM 1234/SP"]),
    ];

    public static byte[] Png(string[] lines, SKTypeface typeface)
    {
        using var surface = SKSurface.Create(new SKImageInfo(1240, 1754));
        surface.Canvas.Clear(SKColors.White);
        using var font = new SKFont(typeface, 32);
        using var ink = new SKPaint { Color = SKColors.Black, IsAntialias = true };
        float y = 120;
        foreach (var line in lines) { surface.Canvas.DrawText(line, 90, y, SKTextAlign.Left, font, ink); y += 56; }
        using var image = surface.Snapshot();
        return image.Encode(SKEncodedImageFormat.Png, 90).ToArray();
    }
}
