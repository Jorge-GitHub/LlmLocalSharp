using LlmLocalSharp.Core.Entities.Clients;
using LlmLocalSharp.Core.Entities.Clients.Messages.ContentParts;
using LlmLocalSharp.Core.Entities.Constants.Enums;

namespace LlmLocalSharp.Core.Clients;

/// <summary>
/// Manages an ordered list of chat messages with support for
/// system message pinning, turn trimming, and history reset.
/// </summary>
public sealed class ChatHistory
{
    private readonly List<ChatMessage> _messages = new();

    /// <summary>Read-only view of all messages in the conversation.</summary>
    public IReadOnlyList<ChatMessage> Messages => this._messages;

    /// <summary>Number of user turns in the conversation.</summary>
    public int TurnCount
    {
        get
        {
            int count = 0;
            foreach (ChatMessage message in this._messages)
            {
                if (message.Role == ChatMessageRole.User)
                {
                    count++;
                }
            }

            return count;
        }
    }

    /// <summary>
    /// Sets the system message. Always kept at index 0.
    /// Replaces any existing system message.
    /// </summary>
    public void AddSystemMessage(string content)
    {
        if (content is null)
        {
            throw new ArgumentNullException(nameof(content));
        }

        if (this._messages.Count > 0 && this._messages[0].Role == ChatMessageRole.System)
        {
            this._messages[0] = new ChatMessage(ChatMessageRole.System, content);
        }
        else
        {
            this._messages.Insert(0, new ChatMessage(ChatMessageRole.System, content));
        }
    }

    /// <summary>Appends a user message to the conversation.</summary>
    public void AddUserMessage(string content)
    {
        if (content is null)
        {
            throw new ArgumentNullException(nameof(content));
        }

        this._messages.Add(new ChatMessage(ChatMessageRole.User, content));
    }

    /// <summary>Appends a user message with multi-modal content parts to the conversation.</summary>
    public void AddUserMessage(IReadOnlyList<ChatMessageContentPart> contentParts)
    {
        if (contentParts is null)
        {
            throw new ArgumentNullException(nameof(contentParts));
        }

        this._messages.Add(new ChatMessage(ChatMessageRole.User, contentParts));
    }

    /// <summary>Appends an assistant message to the conversation.</summary>
    public void AddAssistantMessage(string content)
    {
        if (content is null)
        {
            throw new ArgumentNullException(nameof(content));
        }

        this._messages.Add(new ChatMessage(ChatMessageRole.Assistant, content));
    }

    /// <summary>
    /// Removes the oldest user+assistant pairs from the conversation.
    /// Preserves the system message (if present).
    /// </summary>
    /// <returns><c>true</c> if any messages were removed; <c>false</c> otherwise.</returns>
    public bool TrimOldestTurns(int turnsToRemove)
    {
        if (turnsToRemove <= 0)
        {
            return false;
        }

        int startIndex = (this._messages.Count > 0 && this._messages[0].Role == ChatMessageRole.System)
            ? 1 : 0;

        int removedTurns = 0;
        bool anyRemoved = false;

        while (removedTurns < turnsToRemove && startIndex < this._messages.Count)
        {
            // Remove the next non-system message (should be a user message)
            if (this._messages[startIndex].Role == ChatMessageRole.User)
            {
                this._messages.RemoveAt(startIndex);
                anyRemoved = true;

                // Remove the following assistant message if present
                if (startIndex < this._messages.Count &&
                    this._messages[startIndex].Role == ChatMessageRole.Assistant)
                {
                    this._messages.RemoveAt(startIndex);
                }

                removedTurns++;
            }
            else if (this._messages[startIndex].Role == ChatMessageRole.Assistant)
            {
                // Orphaned assistant message — remove it
                this._messages.RemoveAt(startIndex);
                anyRemoved = true;
            }
            else
            {
                startIndex++;
            }
        }

        return anyRemoved;
    }

    /// <summary>Removes all messages from the conversation.</summary>
    public void Clear()
    {
        this._messages.Clear();
    }
}