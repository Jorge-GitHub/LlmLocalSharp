using LlmLocalSharp.Core.Entities.Clients.Responses.Base;

namespace LlmLocalSharp.Core.Entities.Clients.Responses;

public sealed class LocalChatResponse : ChatResponseBase
{
    public int ContextPosition { get; init; }
    public int ContextSize { get; init; }

    public double ContextUtilization => this.ContextSize > 0
        ? (double)this.ContextPosition / this.ContextSize
        : 0.0;
}
