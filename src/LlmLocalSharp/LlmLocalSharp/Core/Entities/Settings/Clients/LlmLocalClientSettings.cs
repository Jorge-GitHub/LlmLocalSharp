using LlmLocalSharp.Core.Entities.Settings.Engines;
using LlmLocalSharp.Core.Entities.Settings.Libraries;

namespace LlmLocalSharp.Core.Entities.Settings.Clients;

public sealed class LlmLocalClientSettings
{
    public LlmRuntimeSettings Runtime { get; set; } = new();
    public LlmGenerationSettings Generation { get; set; } = new();
    public LibrariesSettings Libraries { get; set; } = new();

    /// <summary>Builds immutable session options from the current client settings.</summary>
    public LlmSessionSettings ToSessionOptions()
        => new()
        {
            ModelPath = this.Runtime.ModelPath,
            ContextSize = this.Runtime.ContextSize,
            Threads = this.Runtime.Threads,
            GpuLayers = this.Runtime.GpuLayers,
            Temperature = this.Generation.Temperature,
            TopP = this.Generation.TopP,
            MaxTokens = this.Generation.MaxTokens,
            StopSequences = this.Generation.StopSequences.ToArray(),
            TopK = this.Generation.TopK,
            MinP = this.Generation.MinP,
            RepeatPenalty = this.Generation.RepeatPenalty,
            FrequencyPenalty = this.Generation.FrequencyPenalty,
            PresencePenalty = this.Generation.PresencePenalty,
            PenaltyLastN = this.Generation.PenaltyLastN,
            Seed = this.Generation.Seed,
            ChatTemplate = this.Generation.ChatTemplate,
            SystemPrompt = this.Generation.SystemPrompt,
        };
}
