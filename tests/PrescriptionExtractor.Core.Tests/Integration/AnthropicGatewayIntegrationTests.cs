using Anthropic;
using FluentAssertions;
using PrescriptionExtractor.Core.Extraction;
using PrescriptionExtractor.Core.Validation;

namespace PrescriptionExtractor.Core.Tests.Integration;

public class AnthropicGatewayIntegrationTests
{
    [SkippableFact]
    public async Task Extracts_a_dataset_prescription_with_haiku()
    {
        Skip.IfNot(AnthropicCredentials.Available(),
            "Rode `ant auth login` ou defina ANTHROPIC_API_KEY para rodar o teste de integração.");

        var file = Path.Combine(RepoRoot(), "dataset", "rx-01-typed.pdf");
        var extractor = new ClaudePrescriptionExtractor(
            new AnthropicMessagesGateway(new AnthropicClient()), new PrescriptionValidator(TimeProvider.System),
            TimeProvider.System);

        var result = await extractor.ExtractAsync(await File.ReadAllBytesAsync(file), "application/pdf", "haiku",
            CancellationToken.None);

        result.Prescription.IsPrescription.Should().BeTrue();
        result.Prescription.Items.Should().NotBeEmpty();
        result.InputTokens.Should().BeGreaterThan(0);
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "ExtratorReceitas.sln"))) dir = dir.Parent;
        return dir?.FullName ?? throw new DirectoryNotFoundException("Raiz do repositório não encontrada.");
    }
}
