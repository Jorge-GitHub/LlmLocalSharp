namespace LlmLocalSharp.Core.Entities.Clients;

/// <summary>
/// Immutable metadata about a loaded LLM model.
/// Populated once at model load time from llama.cpp native calls.
/// </summary>
public sealed class LlmModelMetadata
{
    public required string ModelPath { get; init; }
    public required string Description { get; init; }
    public int VocabSize { get; init; }
    public int TrainingContextLength { get; init; }
    public ulong FileSizeBytes { get; init; }
    public ulong ParameterCount { get; init; }
    public int EmbeddingDimension { get; init; }
}
