namespace LlmLocalSharp.Core.Entities.Clients.Responses;

public sealed class ToolCall
{
    public string Name { get; init; } = string.Empty;
    public string ArgumentsJson { get; init; } = string.Empty;
}