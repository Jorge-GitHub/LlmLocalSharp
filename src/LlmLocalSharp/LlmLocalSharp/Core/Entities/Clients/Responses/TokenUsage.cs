namespace LlmLocalSharp.Core.Entities.Clients.Responses;

public sealed class TokenUsage
{
    public int PromptTokens { get; init; }
    public int CompletionTokens { get; init; }
    public int TotalTokens => this.PromptTokens + this.CompletionTokens;
}
