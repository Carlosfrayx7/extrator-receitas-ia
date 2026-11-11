using PrescriptionExtractor.Core.Domain;

namespace DatasetGenerator;

public sealed class FakeData(int seed)
{
    private readonly Random _rng = new(seed);

    private static readonly string[] Doctors =
        ["Dra. Helena Prado", "Dr. Marcos Vilela", "Dra. Beatriz Nogueira", "Dr. Rafael Teixeira", "Dra. Livia Amaral",
         "Dr. Caio Moreira", "Dra. Sofia Rangel", "Dr. Andre Paiva"];
    private static readonly string[] Patients =
        ["Joao Ficticio", "Maria Exemplo", "Pedro Demonstracao", "Ana Teste", "Lucas Modelo", "Clara Simulada",
         "Bruno Amostra", "Julia Prototipo", "Davi Ensaio", "Laura Piloto"];
    private static readonly string[] States = ["SP", "RJ", "MG", "SC", "RS", "PR", "BA", "PE", "GO", "DF"];

    private static readonly (string Name, string Strength, string Form, string Dosage, string Duration, string Qty, bool Antimicrobial)[] Medications =
    [
        ("Amoxicilina", "500 mg", "cápsula", "1 cápsula de 8 em 8 horas", "7 dias", "21 cápsulas", true),
        ("Azitromicina", "500 mg", "comprimido", "1 comprimido ao dia", "3 dias", "3 comprimidos", true),
        ("Dipirona", "500 mg", "comprimido", "1 comprimido de 6 em 6 horas se dor", "3 dias", "12 comprimidos", false),
        ("Losartana", "50 mg", "comprimido", "1 comprimido pela manhã", "uso contínuo", "30 comprimidos", false),
        ("Omeprazol", "20 mg", "cápsula", "1 cápsula em jejum", "30 dias", "30 cápsulas", false),
        ("Paracetamol", "750 mg", "comprimido", "1 comprimido de 8 em 8 horas se febre", "5 dias", "15 comprimidos", false),
        ("Ibuprofeno", "400 mg", "comprimido", "1 comprimido de 8 em 8 horas após refeição", "5 dias", "15 comprimidos", false),
        ("Metformina", "850 mg", "comprimido", "1 comprimido após almoço e jantar", "uso contínuo", "60 comprimidos", false),
        ("Loratadina", "10 mg", "comprimido", "1 comprimido ao dia", "10 dias", "10 comprimidos", false),
        ("Cefalexina", "500 mg", "cápsula", "1 cápsula de 6 em 6 horas", "7 dias", "28 cápsulas", true),
    ];

    public Prescription NextPrescription(DateOnly referenceDate)
    {
        var meds = Medications.OrderBy(_ => _rng.Next()).Take(_rng.Next(1, 4)).ToList();
        var type = meds.Any(m => m.Antimicrobial) ? PrescriptionType.Antimicrobiano
            : _rng.Next(5) == 0 ? PrescriptionType.ControleEspecial : PrescriptionType.Simples;
        var daysAgo = _rng.Next(6) == 0 ? _rng.Next(40, 90) : _rng.Next(0, 20);

        return new Prescription(
            true, type,
            new Prescriber(F(Pick(Doctors)), F(_rng.Next(1000, 9_999_999).ToString()), F(Pick(States))),
            F(Pick(Patients)),
            F(referenceDate.AddDays(-daysAgo).ToString("yyyy-MM-dd")),
            meds.Select(m => new PrescriptionItem(F(m.Name), F(m.Strength), F(m.Form), F(m.Dosage), F(m.Duration), F(m.Qty)))
                .ToList());
    }

    private string Pick(string[] values) => values[_rng.Next(values.Length)];
    private static ExtractedField F(string value) => new(value, Confidence.High);

    public static Prescription NotAPrescription() => new(
        false, PrescriptionType.Desconhecido,
        new Prescriber(ExtractedField.Missing, ExtractedField.Missing, ExtractedField.Missing),
        ExtractedField.Missing, ExtractedField.Missing, []);
}
