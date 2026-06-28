namespace LlmLocalSharp.Core.Entities.Clients.Responses;

public sealed class LocalEmbeddingResponse
{
    public required float[] Vector { get; init; }
    public int Dimension { get; init; }
}