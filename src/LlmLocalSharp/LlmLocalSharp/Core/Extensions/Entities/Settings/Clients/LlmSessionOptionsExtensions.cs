using LlmLocalSharp.Core.Entities.Settings.Clients;

namespace LlmLocalSharp.Core.Extensions.Entities.Settings.Clients;

internal static class LlmSessionOptionsExtensions
{
    /// <summary>
    /// Penalties (only add if any penalty is active).
    /// </summary>
    /// <param name="options"></param>
    /// <returns></returns>
    internal static bool HasPenalties(this LlmSessionSettings options)
    {
        return options.RepeatPenalty != 1.0f
            || options.FrequencyPenalty != 0.0f
            || options.PresencePenalty != 0.0f;
    }
}

