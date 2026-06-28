using LlmLocalSharp.Core.Entities.Clients;
using LlmLocalSharp.Core.Entities.Clients.Messages.ContentParts;

namespace LlmLocalSharp.Core.Clients.Interfaces;

/// <summary>
/// Formats chat messages into engine-consumable prompt strings.
/// </summary>
public interface IChatTemplate
{
    /// <summary>Formats the full conversation history into a single prompt string.</summary>
    string FormatPrompt(IReadOnlyList<ChatMessage> messages);

    /// <summary>Formats a single new user turn for incremental submission.</summary>
    string FormatUserTurn(string userMessage);

    /// <summary>
    /// Formats a single new user turn from multi-modal content parts.
    /// Non-text parts are silently ignored since local models are text-only.
    /// </summary>
    string FormatUserTurn(IReadOnlyList<ChatMessageContentPart> contentParts);
}
