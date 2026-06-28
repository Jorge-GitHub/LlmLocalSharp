using LlmLocalSharp.Core.Entities.Clients;

namespace LlmLocalSharp.Core.Clients.Interfaces;

/// <summary>
/// Low-level embedding session that owns native resources.
/// Produces a float vector for a given text input.
/// </summary>
public interface ILlmEmbeddingSession : IDisposable
{
    /// <summary>Tokenizes and encodes the text, returning the pooled embedding vector.</summary>
    ReadOnlySpan<float> Embed(string text);

    /// <summary>Number of floats in each embedding vector.</summary>
    int EmbeddingDimension { get; }

    /// <summary>Metadata about the loaded model.</summary>
    LlmModelMetadata ModelMetadata { get; }
}