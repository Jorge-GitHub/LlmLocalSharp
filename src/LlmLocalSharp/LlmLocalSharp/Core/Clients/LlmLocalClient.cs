using LlmLocalSharp.Core.Clients.Interfaces;
using LlmLocalSharp.Core.Clients.Templates;
using LlmLocalSharp.Core.Engines.Repositories;
using LlmLocalSharp.Core.Entities.Settings.Clients;

namespace LlmLocalSharp.Core.Clients;

/// <summary>
/// Entry point for consumers. Create one LlmClient per configuration,
/// then call <see cref="CreateSession"/> for each conversation.
/// </summary>
public sealed class LlmLocalClient : IDisposable
{
    private readonly EngineRepository _repository = new();

    /// <summary>Creates a new chat session (owns its own native context).</summary>
    public LlmSession CreateSession(LlmLocalClientSettings settings)
    {
        IChatTemplate template = ChatTemplateFactory.Create(
            settings.Generation.ChatTemplate);

        return new LlmSession(
            this._repository,
            this._repository.GetEngine(settings),
            template,
            settings.Generation.SystemPrompt);
    }

    /// <summary>Creates a new embedding session (owns its own native context).</summary>
    public EmbeddingSession CreateEmbeddingSession(LlmLocalClientSettings settings)
    {
        return new EmbeddingSession(this._repository.GetEngine(settings));
    }

    /// <summary>Disposes engines cached by this client.</summary>
    public void Dispose()
    {
        this._repository.Dispose();
    }
}

