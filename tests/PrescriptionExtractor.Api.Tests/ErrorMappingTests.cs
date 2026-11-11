using FluentAssertions;
using Microsoft.AspNetCore.Http;

namespace PrescriptionExtractor.Api.Tests;

public class ErrorMappingTests
{
    [Fact]
    public void Body_above_limit_maps_to_413()
    {
        var (status, message) = ErrorMapping.Map(new BadHttpRequestException("grande", 413));
        status.Should().Be(413);
        message.Should().Be("O arquivo passa de 10 MB.");
    }

    [Fact]
    public void Other_bad_request_maps_to_400()
    {
        var (status, message) = ErrorMapping.Map(new BadHttpRequestException("ruim", 400));
        status.Should().Be(400);
        message.Should().Be("Requisição inválida.");
    }

    [Theory]
    [InlineData(typeof(TaskCanceledException))]
    [InlineData(typeof(TimeoutException))]
    public void Timeouts_map_to_504(Type type)
    {
        var (status, _) = ErrorMapping.Map((Exception)Activator.CreateInstance(type)!);
        status.Should().Be(504);
    }
}
