using LlmLocalSharp.Core.Clients.Interfaces;
using LlmLocalSharp.Core.Clients.Templates;
using LlmLocalSharp.Core.Engines.Base;
using LlmLocalSharp.Core.Engines.Repositories;
using LlmLocalSharp.Core.Entities.Settings.Clients;

namespace LlmLocalSharp.Core.Clients;

/// <summary>
/// A convenience wrapper that pairs an engine with a chat session.
/// One LlmSession = one conversation.
/// </summary>
public sealed class LlmSession : IDisposable
{
    private readonly EngineRepository _repository;

    public LlmEngineBase Engine { get; private set; }
    public ChatSession Chat { get; }

    /// <summary>Creates a chat session wrapper around an engine and template.</summary>
    internal LlmSession(EngineRepository repository, LlmEngineBase engine, IChatTemplate template, string? systemPrompt = null)
    {
        this._repository = repository ?? throw new ArgumentNullException(nameof(repository));
        this.Engine = engine ?? throw new ArgumentNullException(nameof(engine));
        this.Chat = new ChatSession(engine, template, systemPrompt);
    }

    /// <summary>
    /// Switches to a different model while preserving the conversation history.
    /// Any active generation completes before the switch occurs.
    /// </summary>
    public void SwitchModel(LlmLocalClientSettings newOptions)
    {
        LlmEngineBase newEngine = this._repository.GetEngine(newOptions);
        IChatTemplate newTemplate = ChatTemplateFactory.Create(
            newOptions.Generation.ChatTemplate);

        this.Chat.SwitchEngine(newEngine, newTemplate);
        this.Engine = newEngine;
    }

    /// <summary>Disposes the active chat session.</summary>
    public void Dispose() => this.Chat.Dispose();
}

