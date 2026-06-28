using LlmLocalSharp.Core.Entities.Constants.Enums;

namespace LlmLocalSharp.Core.Entities.Clients.Responses.Base;

public abstract class ChatResponseBase
{
    public Guid ChatResponseId { get; init; } = Guid.NewGuid();
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public required string Content { get; init; }
    public required StopReason StopReason { get; init; }
    public required TokenUsage Usage { get; init; }
    public required LlmTimings Timings { get; init; }
    public IReadOnlyList<ToolCall>? ToolCalls { get; init; }
    public object? Raw { get; init; }
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }
}

