namespace PrescriptionExtractor.Core.Extraction;

public static class ClaudeModels
{
    public const string Opus = "claude-opus-5-5";
    public const string Haiku = "claude-haiku-5-5";

    public static string Resolve(string? nameOrId) => nameOrId?.Trim().ToLowerInvariant() switch
    {
        null or "" or "opus" or Opus => Opus,
        "haiku" or Haiku => Haiku,
        _ => throw new UnsupportedModelException(nameOrId!),
    };
}
