namespace PrescriptionExtractor.Core.Extraction;

public static class AnthropicCredentials
{
    public static bool Available()
    {
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")) ||
            !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ANTHROPIC_AUTH_TOKEN")))
            return true;

        var configDirs = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Anthropic"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "anthropic"),
        };
        return configDirs.Select(d => Path.Combine(d, "credentials"))
            .Any(d => Directory.Exists(d) && Directory.EnumerateFiles(d, "*.json").Any());
    }
}
