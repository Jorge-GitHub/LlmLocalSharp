namespace LlmLocalSharp.Core.Entities.Clients.Responses;

public sealed class ChatStreamChunk
{
    public required string Text { get; init; }
    public int CompletionTokens { get; init; }
    public float Probability { get; init; }
}