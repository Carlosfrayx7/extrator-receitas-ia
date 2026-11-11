using Anthropic.Core;

namespace PrescriptionExtractor.Core.Extraction;

/// <summary>
/// O SDK expõe stop_reason e category como ApiEnum; o extrator compara com as strings brutas da API
/// ("end_turn", "max_tokens", "refusal"). <c>ApiEnum.Raw()</c> devolve exatamente o valor do JSON,
/// ao contrário de <c>ToString()</c>/enum C# (<c>EndTurn</c>).
/// </summary>
public static class WireValue
{
    public static string? Of<TEnum>(ApiEnum<string, TEnum>? value) where TEnum : struct, Enum =>
        value?.Raw();
}
