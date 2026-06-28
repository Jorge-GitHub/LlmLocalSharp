using LlmLocalSharp.Core.Entities.Constants.Enums;
using LlmLocalSharp.Engines.Providers.LlamaService.Clients.Helpers;
using System.Text;

namespace LlmLocalSharp.Engines.Providers.LlamaService.Clients;

internal sealed class LlamaChatSessionStatus
{
    private readonly LlamaChatSessionStatusHelper _helper = new();
    private readonly StringBuilder _recentOutput = new();
    public int Position { get; set; }
    public int GeneratedCount { get; set; }
    public bool HasPending { get; set; }
    public int PendingToken { get; set; }
    public bool Done { get; set; }
    public int PromptTokenCount { get; set; }
    public StopReason StopReason { get; set; }
    public float LastTokenProbability { get; set; }

    /// <summary>Resets all prompt and generation state.</summary>
    public void Reset()
    {
        this.Position = 0;
        this.GeneratedCount = 0;
        this.HasPending = false;
        this.PendingToken = 0;
        this.Done = false;
        this._recentOutput.Clear();
        this.PromptTokenCount = 0;
        this.StopReason = StopReason.None;
    }

    /// <summary>Marks generation as stopped because the max token limit was reached.</summary>
    public void SetMaxTokensReached()
    {
        this.Done = true;
        this.StopReason = StopReason.MaxTokensReached;
    }

    /// <summary>Marks generation as stopped because an end-of-generation token was reached.</summary>
    public void SetEndSequence()
    {
        this.Done = true;
        this.StopReason = StopReason.EndOfSequence;
        this.LastTokenProbability = 0f;
    }

    /// <summary>Clears a pending token that produced no decoded text.</summary>
    public void SetTokenWithNoText()
    {
        this.HasPending = false;
        this.PendingToken = 0;
        this.GeneratedCount++;
    }

    /// <summary>Resets generation-only state before submitting a new turn.</summary>
    public void ResetGenerationStateForANewTurn()
    {
        this.HasPending = false;
        this.PendingToken = 0;
        this.GeneratedCount = 0;
        this.Done = false;
        this._recentOutput.Clear();
        this.PromptTokenCount = 0;
        this.StopReason = StopReason.None;
    }

    /// <summary>Loads initial status values for a newly created session.</summary>
    public void LoadDefaultValues()
    {
        this.Position = 0;
        this.GeneratedCount = 0;
        this.HasPending = false;
        this.PendingToken = 0;
        this.Done = false;
    }

    /// <summary>
    /// Appends decoded characters to the recent output buffer
    /// used for stop-sequence detection.
    /// </summary>
    public void AppendToRecentOutput(ReadOnlySpan<char> characters)
    {
        this._recentOutput.Append(characters);
    }

    /// <summary>
    /// Compute probability via log-sum-exp.
    /// </summary>
    public void ComputeProbabilityViaLogSumExp(
        ReadOnlySpan<float> logits, int token)
    {
        this.LastTokenProbability = this._helper
            .ComputeProbabilityViaLogSumExp(logits, token);
    }

    /// <summary>Checks recent output for configured stop sequences.</summary>
    public bool KeepSequences(IReadOnlyList<string> stopSequences,
        int maxStopSequenceLength)
    {
        this.TrimRecentOutput(maxStopSequenceLength);

        foreach (string stop in stopSequences)
        {
            if (this.RecentOutputContains(stop))
            {
                this.Done = true;
                this.StopReason = StopReason.StopSequenceMatched;

                return false;
            }
        }

        return true;
    }

    /// <summary>Trims the recent output buffer to the stop-sequence search window.</summary>
    private void TrimRecentOutput(int maxStopSequenceLength)
    {
        int maxLength = maxStopSequenceLength * 2;

        if (this._recentOutput.Length > maxLength)
        {
            int removeCount = this._recentOutput.Length - maxLength;
            this._recentOutput.Remove(0, removeCount);
        }
    }

    /// <summary>Returns whether recent output contains the specified stop sequence.</summary>
    private bool RecentOutputContains(string value)
    {
        int length = this._recentOutput.Length;

        if (length == 0)
        {
            return false;
        }

        Span<char> buffer = length <= 512
            ? stackalloc char[length]
            : new char[length];
        this._recentOutput.CopyTo(0, buffer, length);

        return ((ReadOnlySpan<char>)buffer)
            .IndexOf(value.AsSpan(), StringComparison.Ordinal) >= 0;
    }
}
