using LlmLocalSharp.Core.Entities.Constants.Enums;

namespace LlmLocalSharp.Core.Entities.Settings.Clients;

// ──────────────────────────────────────────────
// Per-session options (immutable snapshot)
// ──────────────────────────────────────────────

public sealed class LlmSessionSettings
{
    public required string ModelPath { get; init; }
    public int ContextSize { get; init; }
    public int Threads { get; init; }
    public int GpuLayers { get; init; }
    public float Temperature { get; init; }
    public float TopP { get; init; }
    public int MaxTokens { get; init; }
    public int TopK { get; init; } = 40;
    public float MinP { get; init; } = 0.0f;
    public float RepeatPenalty { get; init; } = 1.0f;
    public float FrequencyPenalty { get; init; } = 0.0f;
    public float PresencePenalty { get; init; } = 0.0f;
    public int PenaltyLastN { get; init; } = 64;
    public uint Seed { get; init; } = uint.MaxValue;
    public IReadOnlyList<string> StopSequences { get; init; } = [];
    public ChatTemplateType ChatTemplate { get; init; } = ChatTemplateType.Llama2;
    public string? SystemPrompt { get; init; } = null;
}
