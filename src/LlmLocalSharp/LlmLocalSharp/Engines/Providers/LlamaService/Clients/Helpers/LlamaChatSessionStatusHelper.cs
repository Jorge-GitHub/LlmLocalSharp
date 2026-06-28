namespace LlmLocalSharp.Engines.Providers.LlamaService.Clients.Helpers;

internal sealed class LlamaChatSessionStatusHelper
{
    /// <summary>
    /// Compute probability via log-sum-exp.
    /// </summary>
    public float ComputeProbabilityViaLogSumExp(
        ReadOnlySpan<float> logits, int token)
    {
        float tokenLogit = logits[token];
        float maxLogit = this.GetMaxLogit(logits);
        float sumExp = this.GetSumExp(logits, maxLogit);

        return MathF.Exp(tokenLogit - maxLogit) / sumExp;
    }

    /// <summary>Finds the maximum logit value for stable probability calculation.</summary>
    private float GetMaxLogit(ReadOnlySpan<float> logits)
    {
        float maxLogit = float.MinValue;

        for (int i = 0; i < logits.Length; i++)
        {
            if (logits[i] > maxLogit)
            {
                maxLogit = logits[i];
            }
        }

        return maxLogit;
    }

    /// <summary>Sums exponentials normalized by the maximum logit.</summary>
    private float GetSumExp(ReadOnlySpan<float> logits,
        float maxLogit)
    {
        float sumExp = 0f;

        for (int i = 0; i < logits.Length; i++)
        {
            sumExp += MathF.Exp(logits[i] - maxLogit);
        }

        return sumExp;
    }
}
