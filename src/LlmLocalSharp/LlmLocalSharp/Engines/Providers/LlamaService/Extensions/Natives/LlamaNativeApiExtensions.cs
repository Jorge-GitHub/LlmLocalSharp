using LlmLocalSharp.Core.Entities.Settings.Clients;
using LlmLocalSharp.Core.Extensions.Entities.Settings.Clients;
using LlmLocalSharp.Core.Interop;
using LlmLocalSharp.Engines.Providers.LlamaService.Natives;

namespace LlmLocalSharp.Engines.Providers.LlamaService.Extensions.Natives;

internal static class LlamaNativeApiExtensions
{
    /// <summary>Initializes the sampler chain for a chat session.</summary>
    public static void InitChatSession(this LlamaNativeApi native,
        LlmSessionSettings options, LlmSafeHandle sampler)
    {
        native.InitPenalties(options, sampler);
        native.InitTopSamplers(options, sampler);
        native.InitMinP(options, sampler);
        native.InitTemperature(options, sampler);
        // Dist (always last)
        native.InitDistribution(options, sampler);
    }

    /// <summary>Adds penalty sampling when penalty settings are enabled.</summary>
    internal static void InitPenalties(this LlamaNativeApi native,
        LlmSessionSettings options, LlmSafeHandle sampler)
    {
        if (options.HasPenalties())
        {
            IntPtr penaltiesSampler =
                native.SamplerInitPenalties(
                options.PenaltyLastN,
                options.RepeatPenalty,
                options.FrequencyPenalty,
                options.PresencePenalty);

            native.SamplerChainAdd(
                sampler.DangerousGetHandle(), penaltiesSampler);
        }
    }

    /// <summary>Adds top-k and top-p samplers to the sampler chain.</summary>
    internal static void InitTopSamplers(this LlamaNativeApi native,
        LlmSessionSettings options, LlmSafeHandle sampler)
    {
        // Top-K
        IntPtr topKSampler = native.SamplerInitTopK(options.TopK);
        native.SamplerChainAdd(sampler.DangerousGetHandle(), topKSampler);

        // Top-P
        IntPtr topPSampler = native.SamplerInitTopP(options.TopP, minKeep: 1);
        native.SamplerChainAdd(sampler.DangerousGetHandle(), topPSampler);
    }

    /// <summary>Adds min-p sampling when configured.</summary>
    internal static void InitMinP(this LlamaNativeApi native,
        LlmSessionSettings options, LlmSafeHandle sampler)
    {
        // Min-P (only add if active)
        if (options.MinP > 0.0f)
        {
            IntPtr minPSampler = native.SamplerInitMinP(
                options.MinP, minKeep: 1);
            native.SamplerChainAdd(
                sampler.DangerousGetHandle(), minPSampler);
        }
    }

    /// <summary>Adds temperature sampling to the sampler chain.</summary>
    internal static void InitTemperature(this LlamaNativeApi native,
        LlmSessionSettings options, LlmSafeHandle sampler)
    {
        IntPtr tempSampler = native.SamplerInitTemp(options.Temperature);
        native.SamplerChainAdd(sampler.DangerousGetHandle(), tempSampler);
    }

    /// <summary>Adds the distribution sampler as the final sampler in the chain.</summary>
    internal static void InitDistribution(this LlamaNativeApi native,
        LlmSessionSettings options, LlmSafeHandle sampler)
    {
        IntPtr distSampler = native.SamplerInitDist(
            options.Seed);
        native.SamplerChainAdd(
            sampler.DangerousGetHandle(), distSampler);
    }
}
