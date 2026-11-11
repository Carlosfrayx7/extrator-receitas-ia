using Anthropic.Exceptions;
using PrescriptionExtractor.Core.Extraction;

namespace PrescriptionExtractor.Api;

public static class ErrorMapping
{
    public static (int Status, string Message) Map(Exception ex) => ex switch
    {
        UnsupportedFileException e => (400, e.Message),
        UnsupportedModelException e => (400, e.Message),
        InvalidDataException => (400, "Formulário multipart inválido. Envie o arquivo no campo \"file\"."),
        ModelRefusalException => (422, "O modelo não processou este documento. Envie uma receita médica."),
        AnthropicRateLimitException => (503, "Limite de requisições da API atingido. Tente de novo em instantes."),
        OutputTruncatedException or InvalidModelOutputException =>
            (502, "O modelo devolveu uma resposta incompleta. Tente de novo."),
        AnthropicApiException => (502, "Erro ao chamar a API da Anthropic. Tente de novo."),
        _ => (500, "Erro inesperado."),
    };
}
