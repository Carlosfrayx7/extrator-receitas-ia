namespace PrescriptionExtractor.Core.Extraction;

public sealed class UnsupportedFileException(string message) : Exception(message);
public sealed class UnsupportedModelException(string model) : Exception($"Modelo não suportado: {model}.");
public sealed class ModelRefusalException(string? category)
    : Exception("O modelo recusou processar o documento.") { public string? Category { get; } = category; }
public sealed class OutputTruncatedException() : Exception("A resposta do modelo foi cortada (max_tokens).");
public sealed class InvalidModelOutputException(string message, Exception? inner = null) : Exception(message, inner);
