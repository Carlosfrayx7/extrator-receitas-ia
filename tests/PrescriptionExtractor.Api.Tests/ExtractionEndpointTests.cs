using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PrescriptionExtractor.Core.Extraction;
using PrescriptionExtractor.Core.Validation;

namespace PrescriptionExtractor.Api.Tests;

public class ExtractionEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly FakeExtractor _fake = new();
    private readonly HttpClient _client;

    public ExtractionEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(b => b.ConfigureServices(s =>
        {
            s.RemoveAll<IPrescriptionExtractor>();
            s.AddSingleton<IPrescriptionExtractor>(_fake);
        })).CreateClient();
    }

    private static MultipartFormDataContent Upload(byte[] bytes, string name, string type, string? model = null)
    {
        var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(type);
        form.Add(file, "file", name);
        if (model is not null) form.Add(new StringContent(model), "model");
        return form;
    }

    [Fact]
    public async Task Returns_200_with_result()
    {
        var response = await _client.PostAsync("/extractions", Upload([1, 2], "r.png", "image/png", "haiku"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("status").GetString().Should().Be("valid");
        json.GetProperty("prescription").GetProperty("prescriptionType").GetString().Should().Be("simples");
        _fake.LastModel.Should().Be("haiku");
    }

    [Fact]
    public async Task Unsupported_type_returns_400()
    {
        var response = await _client.PostAsync("/extractions", Upload([1], "r.gif", "image/gif"));
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("error").GetString()
            .Should().Contain("PNG");
    }

    [Fact]
    public async Task Missing_file_returns_400()
    {
        var response = await _client.PostAsync("/extractions", new MultipartFormDataContent());
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Not_a_prescription_returns_422()
    {
        _fake.Behavior = () => FakeExtractor.Ok() with { Status = ExtractionStatus.Invalid,
            Issues = [new ValidationIssue("not_a_prescription", "isPrescription", "x", Severity.Error)] };
        var response = await _client.PostAsync("/extractions", Upload([1], "r.png", "image/png"));
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Refusal_returns_422()
    {
        _fake.Behavior = () => throw new ModelRefusalException("general_harms");
        var response = await _client.PostAsync("/extractions", Upload([1], "r.png", "image/png"));
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Unknown_model_returns_400()
    {
        _fake.Behavior = () => throw new UnsupportedModelException("gpt");
        var response = await _client.PostAsync("/extractions", Upload([1], "r.png", "image/png", "gpt"));
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Health_reports_key_presence_without_value()
    {
        var json = await _client.GetFromJsonAsync<JsonElement>("/health");
        json.GetProperty("status").GetString().Should().Be("ok");
        json.TryGetProperty("credentialsConfigured", out _).Should().BeTrue();
        json.GetRawText().Should().NotContain("sk-ant");
    }

    [Fact]
    public void Rate_limit_maps_to_503_and_api_errors_to_502()
    {
        ErrorMapping.Map(new Anthropic.Exceptions.AnthropicRateLimitException(new HttpRequestException("x"))
            { StatusCode = HttpStatusCode.TooManyRequests, ResponseBody = "" }).Status.Should().Be(503);
        ErrorMapping.Map(new InvalidModelOutputException("x")).Status.Should().Be(502);
        ErrorMapping.Map(new OutputTruncatedException()).Status.Should().Be(502);
    }
}
