namespace LlmLocalSharp.Core.Entities.Clients.Responses;

public sealed class LlmTimings
{
    public TimeSpan TotalDuration { get; init; }
    public TimeSpan? PromptProcessingTime { get; init; }
    public TimeSpan? GenerationTime { get; init; }
    public double? TokensPerSecond { get; init; }
    public double? PromptTokensPerSecond { get; init; }
}

