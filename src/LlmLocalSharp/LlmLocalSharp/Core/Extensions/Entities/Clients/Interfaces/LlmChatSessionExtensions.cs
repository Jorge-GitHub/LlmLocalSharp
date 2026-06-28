using LlmLocalSharp.Core.Clients.Interfaces;
using LlmLocalSharp.Core.Entities.Clients.Responses;
using System.Diagnostics;

namespace LlmLocalSharp.Core.Extensions.Entities.Clients.Interfaces;

internal static class LlmChatSessionExtensions
{
    /// <summary>Creates a complete chat response from native session state and timing data.</summary>
    internal static LocalChatResponse ToChatResponse(
        this ILlmChatSession session,
        string fullResponse, int completionTokens,
        Stopwatch totalStopwatch, Stopwatch promptStopwatch,
        Stopwatch generationStopwatch)
    {
        TimeSpan generationTime = generationStopwatch.Elapsed;
        TimeSpan promptTime = promptStopwatch.Elapsed;

        double? tokensPerSecond = generationTime.TotalSeconds > 0
            ? completionTokens / generationTime.TotalSeconds
            : null;

        double? promptTokensPerSecond = promptTime.TotalSeconds > 0
            ? session.PromptTokenCount / promptTime.TotalSeconds
            : null;

        return new LocalChatResponse
        {
            Content = fullResponse,
            StopReason = session!.StopReason,
            Usage = new TokenUsage
            {
                PromptTokens = session!.PromptTokenCount,
                CompletionTokens = completionTokens
            },
            Timings = new LlmTimings
            {
                TotalDuration = totalStopwatch.Elapsed,
                PromptProcessingTime = promptTime,
                GenerationTime = generationTime,
                TokensPerSecond = tokensPerSecond,
                PromptTokensPerSecond = promptTokensPerSecond
            },
            ContextPosition = session.ContextPosition,
            ContextSize = session.ContextSize
        };
    }
}
