using System.Text.Json;
using System.Text.Json.Nodes;

namespace PrescriptionExtractor.Core.Extraction;

public static class PrescriptionSchema
{
    public const string SystemPrompt = """
        Você extrai dados de receitas médicas brasileiras a partir de imagens ou PDFs.
        Regras:
        - Extração literal: copie o que está escrito. Não complete, não corrija grafia e não deduza valores.
        - Se um campo não existir ou estiver ilegível, devolva value = null e confidence = "low".
        - confidence: "high" quando a leitura é inequívoca, "medium" quando há alguma dúvida, "low" quando é um palpite.
        - issueDate no formato yyyy-MM-dd.
        - prescriptionType: "controle_especial" para receitas de controle especial (azul, amarela ou duas vias),
          "antimicrobiano" quando há antibiótico, "simples" para as demais, "desconhecido" se não der para saber.
        - Se o documento não for uma receita médica, devolva isPrescription = false e campos vazios.
        """;

    public const string UserInstruction = "Extraia os dados desta receita médica conforme o esquema.";

    public static readonly JsonElement Schema = Build();

    private static JsonElement Build()
    {
        JsonObject Field() => Strict(new JsonObject
        {
            ["value"] = new JsonObject { ["type"] = new JsonArray("string", "null") },
            ["confidence"] = new JsonObject { ["type"] = "string", ["enum"] = new JsonArray("high", "medium", "low") },
        });

        var item = Strict(new JsonObject
        {
            ["medication"] = Field(), ["strength"] = Field(), ["form"] = Field(),
            ["dosage"] = Field(), ["duration"] = Field(), ["quantity"] = Field(),
        });

        var root = Strict(new JsonObject
        {
            ["isPrescription"] = new JsonObject { ["type"] = "boolean" },
            ["prescriptionType"] = new JsonObject
            {
                ["type"] = "string",
                ["enum"] = new JsonArray("simples", "controle_especial", "antimicrobiano", "desconhecido"),
            },
            ["prescriber"] = Strict(new JsonObject
            {
                ["name"] = Field(), ["crmNumber"] = Field(), ["crmState"] = Field(),
            }),
            ["patientName"] = Field(),
            ["issueDate"] = Field(),
            ["items"] = new JsonObject { ["type"] = "array", ["items"] = item },
        });

        return JsonSerializer.SerializeToElement(root);
    }

    private static JsonObject Strict(JsonObject properties) => new()
    {
        ["type"] = "object",
        ["properties"] = properties,
        ["required"] = new JsonArray(properties.Select(p => (JsonNode)p.Key).ToArray()),
        ["additionalProperties"] = false,
    };
}
