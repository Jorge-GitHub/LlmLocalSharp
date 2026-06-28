using LlmLocalSharp.Core.Clients.Interfaces;
using LlmLocalSharp.Core.Entities.Clients;
using LlmLocalSharp.Core.Entities.Clients.Messages.ContentParts;
using LlmLocalSharp.Core.Entities.Constants.Enums;
using System.Text;

namespace LlmLocalSharp.Core.Clients.Templates;

/// <summary>
/// Llama-2 chat format:
/// System: &lt;&lt;SYS&gt;&gt;...&lt;&lt;/SYS&gt;&gt; inside the first [INST]
/// User:   [INST] ... [/INST]\n
/// Asst:   {content} &lt;/s&gt;\n
/// </summary>
public sealed class Llama2ChatTemplate : IChatTemplate
{
    /// <summary>Formats a full conversation using the Llama 2 chat template.</summary>
    public string FormatPrompt(IReadOnlyList<ChatMessage> messages)
    {
        StringBuilder builder = new();
        bool systemIncluded = false;
        string? systemContent = null;

        // Extract system message if present
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
                    builder.Append($"<<SYS>>\n{systemContent}\n<</SYS>>\n\n");
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

    /// <summary>Formats a single user message for the next Llama 2 turn.</summary>
    public string FormatUserTurn(string userMessage)
    {
        return $"[INST] {userMessage} [/INST]\n";
    }

    /// <summary>Formats multi-modal content parts for the next Llama 2 turn.</summary>
    public string FormatUserTurn(IReadOnlyList<ChatMessageContentPart> contentParts)
    {
        return this.FormatUserTurn(ContentPartHelper.ExtractText(contentParts));
    }
}
