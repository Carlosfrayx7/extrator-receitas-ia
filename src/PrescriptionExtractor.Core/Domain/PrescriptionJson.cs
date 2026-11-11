using System.Text.Json;
using System.Text.Json.Serialization;

namespace PrescriptionExtractor.Core.Domain;

public static class PrescriptionJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower, allowIntegerValues: false) },
        WriteIndented = true,
    };

    public static Prescription Parse(string json) =>
        JsonSerializer.Deserialize<Prescription>(json, Options)
        ?? throw new JsonException("Resposta vazia.");

    public static string Serialize(Prescription prescription) =>
        JsonSerializer.Serialize(prescription, Options);
}
