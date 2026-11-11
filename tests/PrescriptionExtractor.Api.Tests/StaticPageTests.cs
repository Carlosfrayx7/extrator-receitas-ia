using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PrescriptionExtractor.Api.Tests;

public class StaticPageTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task Root_serves_the_upload_page()
    {
        var html = await factory.CreateClient().GetStringAsync("/");
        html.Should().Contain("id=\"dropzone\"").And.Contain("id=\"model\"").And.Contain("app.js")
            .And.Contain("dados fictícios");
    }
}
