using LlmLocalSharp.Core.Clients.Interfaces;
using LlmLocalSharp.Core.Engines.Base.Interfaces;
using LlmLocalSharp.Core.Entities.Constants.Enums;
using LlmLocalSharp.Core.Entities.Settings.Clients;

namespace LlmLocalSharp.Core.Engines.Base;

public abstract class LlmEngineBase : ILlmEngine
{
    public LlmEngineType Type { get; }
    public LlmBackend Backend { get; }
    public string ModelName { get; }
    protected LlmLocalClientSettings Settings { get; }

    /// <summary>Initializes base engine metadata from the client settings.</summary>
    protected LlmEngineBase(LlmLocalClientSettings settings)
    {
        this.Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        this.Type = settings.Runtime.Engine;
        this.Backend = settings.Runtime.Backend;
        this.ModelName = settings.Runtime.ModelName;
    }

    /// <summary>Creates a native chat session for this engine.</summary>
    public abstract ILlmChatSession CreateSession();

    /// <summary>Creates a native embedding session for this engine.</summary>
    public abstract ILlmEmbeddingSession CreateEmbeddingSession();

    /// <summary>Releases resources owned by the engine.</summary>
    public virtual void Dispose()
    {
    }
}

