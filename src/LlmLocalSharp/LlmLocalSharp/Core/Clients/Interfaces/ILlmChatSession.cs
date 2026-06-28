using LlmLocalSharp.Core.Entities.Clients;
using LlmLocalSharp.Core.Entities.Constants.Enums;

namespace LlmLocalSharp.Core.Clients.Interfaces;

/// <summary>
/// Callback that receives the raw logits array (one float per vocabulary entry)
/// before each sampling step. The span is only valid for the duration of the callback.
/// </summary>
public delegate void LogitsCallback(ReadOnlySpan<float> logits);

/// <summary>
/// Low-level chat session that owns native resources.
/// Produces one token at a time as UTF-8 bytes.
/// </summary>
public interface ILlmChatSession : IDisposable
{
    /// <summary>Submit a pre-formatted prompt into the context.</summary>
    /// <param name="formattedPrompt">The fully formatted prompt string.</param>
    /// <param name="isFirstSubmission">
    /// When <c>true</c>, a BOS (beginning-of-sequence) token is prepended.
    /// </param>
    void SubmitPrompt(string formattedPrompt, bool isFirstSubmission);

    /// <summary>
    /// Sample the next token, decode it to UTF-8, and write it into the buffer.
    /// Returns <c>false</c> when generation is complete (EOS or max tokens).
    /// </summary>
    bool NextToken(Span<byte> utf8Buffer, out int bytesWritten);

    /// <summary>Clears the KV cache and resets the context position to 0.</summary>
    void Reset();

    /// <summary>Current position (number of tokens consumed) in the context.</summary>
    int ContextPosition { get; }

    /// <summary>Total context size (maximum number of tokens).</summary>
    int ContextSize { get; }

    /// <summary>Number of tokens consumed by the last submitted prompt.</summary>
    int PromptTokenCount { get; }

    /// <summary>Reason generation stopped after the last call to <see cref="NextToken"/>.</summary>
    StopReason StopReason { get; }

    /// <summary>Metadata about the loaded model (vocab size, description, parameter count, etc.).</summary>
    LlmModelMetadata ModelMetadata { get; }

    /// <summary>
    /// The probability (0–1) of the token selected during the last <see cref="NextToken"/> call.
    /// Computed from the raw model logits via softmax (pre-sampling-chain).
    /// </summary>
    float LastTokenProbability { get; }

    /// <summary>
    /// Optional callback that fires with the raw logits array before each sampling step.
    /// Set to <c>null</c> to disable. The span passed to the callback is only valid for its duration.
    /// </summary>
    LogitsCallback? OnLogits { get; set; }
}