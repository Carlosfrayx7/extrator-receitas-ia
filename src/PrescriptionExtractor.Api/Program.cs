using System.Text.Json;
using System.Text.Json.Serialization;
using Anthropic;
using PrescriptionExtractor.Api;
using PrescriptionExtractor.Core.Extraction;
using PrescriptionExtractor.Core.Validation;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower));
});
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<PrescriptionValidator>();
builder.Services.AddSingleton(_ => new AnthropicClient());
builder.Services.AddSingleton<IMessagesGateway, AnthropicMessagesGateway>();
builder.Services.AddSingleton<IPrescriptionExtractor, ClaudePrescriptionExtractor>();

var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    credentialsConfigured = AnthropicCredentials.Available(),
}));

app.MapGet("/samples", (IWebHostEnvironment env) =>
{
    var dir = Path.Combine(env.WebRootPath ?? "wwwroot", "samples");
    IEnumerable<string?> files = Directory.Exists(dir)
        ? Directory.GetFiles(dir).Select(Path.GetFileName).Order()
        : [];
    return Results.Ok(files.Select(f => new { name = f, url = $"/samples/{f}" }));
});

app.MapPost("/extractions", async (HttpRequest request, IPrescriptionExtractor extractor, ILogger<Program> log,
    CancellationToken ct) =>
{
    try
    {
        if (!request.HasFormContentType) return Error(400, "Envie o arquivo em multipart/form-data.");
        var form = await request.ReadFormAsync(ct);
        var file = form.Files.GetFile("file");
        if (file is null) return Error(400, "Envie o arquivo da receita no campo \"file\".");
        if (file.Length > FileRules.MaxBytes) return Error(400, "O arquivo passa de 10 MB.");

        var mediaType = FileRules.ResolveMediaType(file.FileName, file.ContentType);
        using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, ct);

        var result = await extractor.ExtractAsync(buffer.ToArray(), mediaType, form["model"].FirstOrDefault(), ct);
        if (result.Issues.Any(i => i.Code == "not_a_prescription"))
            return Error(422, "O documento enviado não é uma receita médica.");

        log.LogInformation("Extração {Status} com {Model}: {In}/{Out} tokens, {Ms} ms",
            result.Status, result.Model, result.InputTokens, result.OutputTokens, result.LatencyMs);
        return Results.Ok(result);
    }
    catch (Exception ex) when (ex is not OperationCanceledException)
    {
        var (status, message) = ErrorMapping.Map(ex);
        log.LogWarning("Extração falhou com {Status}: {Type}", status, ex.GetType().Name);
        return Error(status, message);
    }
}).DisableAntiforgery();

app.Run();

static IResult Error(int status, string message) => Results.Json(new { error = message }, statusCode: status);

public partial class Program;
