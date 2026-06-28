using LlmLocalSharp.Core.Clients.Interfaces;
using LlmLocalSharp.Core.Entities.Clients;
using LlmLocalSharp.Core.Entities.Clients.Messages.ContentParts;
using LlmLocalSharp.Core.Entities.Constants.Enums;
using System.Text;

namespace LlmLocalSharp.Core.Clients.Templates;

/// <summary>
/// ChatML format:
/// &lt;|im_start|&gt;role\ncontent&lt;|im_end|&gt;\n
/// </summary>
public sealed class ChatMlTemplate : IChatTemplate
{
    /// <summary>Formats a full conversation using the ChatML template.</summary>
    public string FormatPrompt(IReadOnlyList<ChatMessage> messages)
    {
        StringBuilder builder = new();

        foreach (ChatMessage message in messages)
        {
            string role = message.Role switch
            {
                ChatMessageRole.System => "system",
                ChatMessageRole.User => "user",
                ChatMessageRole.Assistant => "assistant",
                _ => "user"
            };

            string content = message.ContentParts is not null
                ? ContentPartHelper.ExtractText(message.ContentParts)
                : message.Content;

            builder.Append($"<|im_start|>{role}\n{content}<|im_end|>\n");
        }

        // Add assistant start tag to prompt generation
        builder.Append("<|im_start|>assistant\n");

        return builder.ToString();
    }

    /// <summary>Formats a single user message for the next ChatML turn.</summary>
    public string FormatUserTurn(string userMessage)
    {
        return $"<|im_start|>user\n{userMessage}<|im_end|>\n<|im_start|>assistant\n";
    }

    /// <summary>Formats multi-modal content parts for the next ChatML turn.</summary>
    public string FormatUserTurn(IReadOnlyList<ChatMessageContentPart> contentParts)
    {
        return this.FormatUserTurn(ContentPartHelper.ExtractText(contentParts));
    }
}
