using LlmLocalSharp.Core.Entities.Constants.Enums;

namespace LlmLocalSharp.Core.Entities.Settings.Engines;

// ──────────────────────────────────────────────
// Generation parameters
// ──────────────────────────────────────────────

public sealed class LlmGenerationSettings
{
    public float Temperature { get; set; } = 0.7f;
    public float TopP { get; set; } = 0.9f;
    public int MaxTokens { get; set; } = 512;
    public int TopK { get; set; } = 40;
    public float MinP { get; set; } = 0.0f;
    public float RepeatPenalty { get; set; } = 1.0f;
    public float FrequencyPenalty { get; set; } = 0.0f;
    public float PresencePenalty { get; set; } = 0.0f;
    public int PenaltyLastN { get; set; } = 64;
    public uint Seed { get; set; } = uint.MaxValue; // uint.MaxValue = random
    public List<string> StopSequences { get; set; } = new() { "</s>", "User:", "Assistant:" };
    public ChatTemplateType ChatTemplate { get; set; } = ChatTemplateType.Llama2;
    public string? SystemPrompt { get; set; } = null;
}
