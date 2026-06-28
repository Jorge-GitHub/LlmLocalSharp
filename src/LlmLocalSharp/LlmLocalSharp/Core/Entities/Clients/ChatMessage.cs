using LlmLocalSharp.Core.Entities.Clients.Messages.ContentParts;
using LlmLocalSharp.Core.Entities.Constants.Enums;

namespace LlmLocalSharp.Core.Entities.Clients;

/// <summary>
/// Immutable representation of a single message in a chat conversation.
/// </summary>
public sealed class ChatMessage
{
    public ChatMessageRole Role { get; }
    public string Content { get; }
    public IReadOnlyList<ChatMessageContentPart>? ContentParts { get; }

    /// <summary>Creates a text-only chat message.</summary>
    public ChatMessage(ChatMessageRole role, string content)
    {
        this.Role = role;
        this.Content = content ?? throw new ArgumentNullException(nameof(content));
    }

    /// <summary>Creates a chat message from structured content parts.</summary>
    public ChatMessage(ChatMessageRole role, IReadOnlyList<ChatMessageContentPart> contentParts)
    {
        this.Role = role;
        this.ContentParts = contentParts ?? throw new ArgumentNullException(nameof(contentParts));
        this.Content = string.Empty;
    }
}

