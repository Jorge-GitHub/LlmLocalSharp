using LlmLocalSharp.Core.Clients.Interfaces;
using LlmLocalSharp.Core.Entities.Clients;
using LlmLocalSharp.Core.Entities.Clients.Messages.ContentParts;
using LlmLocalSharp.Core.Entities.Constants.Enums;
using System.Text;

namespace LlmLocalSharp.Core.Clients.Templates;

/// <summary>
/// Mistral format (similar to Llama-2 but system message goes
/// directly inside the first [INST] without &lt;&lt;SYS&gt;&gt; wrapper).
/// </summary>
public sealed class MistralChatTemplate : IChatTemplate
{
    /// <summary>Formats a full conversation using the Mistral chat template.</summary>
    public string FormatPrompt(IReadOnlyList<ChatMessage> messages)
    {
        StringBuilder builder = new();
        bool systemIncluded = false;
        string? systemContent = null;

        foreach (ChatMessage message in messages)
        {
            if (message.Role == ChatMessageRole.System)
            {
                systemContent = message.Content;
                break;
            }
        }

        foreach (ChatMessage message in messages)
        {
            if (message.Role == ChatMessageRole.System)
            {
                continue;
            }

            if (message.Role == ChatMessageRole.User)
            {
                builder.Append("[INST] ");

                if (!systemIncluded && systemContent is not null)
                {
                    builder.Append($"{systemContent}\n\n");
                    systemIncluded = true;
                }

                string content = message.ContentParts is not null
                    ? ContentPartHelper.ExtractText(message.ContentParts)
                    : message.Content;

                builder.Append($"{content} [/INST]\n");
            }
            else if (message.Role == ChatMessageRole.Assistant)
            {
                builder.Append($"{message.Content} </s>\n");
            }
        }

        return builder.ToString();
    }

    /// <summary>Formats a single user message for the next Mistral turn.</summary>
    public string FormatUserTurn(string userMessage)
    {
        return $"[INST] {userMessage} [/INST]\n";
    }

    /// <summary>Formats multi-modal content parts for the next Mistral turn.</summary>
    public string FormatUserTurn(IReadOnlyList<ChatMessageContentPart> contentParts)
    {
        return this.FormatUserTurn(ContentPartHelper.ExtractText(contentParts));
    }
}
