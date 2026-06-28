using LlmLocalSharp.Core.Clients.Interfaces;

namespace LlmLocalSharp.Core.Engines.Base.Interfaces;

/// <summary>
/// Factory for creating native chat and embedding sessions.
/// One engine instance per model/backend combination.
/// </summary>
public interface ILlmEngine : IDisposable
{
    ILlmChatSession CreateSession();
    ILlmEmbeddingSession CreateEmbeddingSession();
}

