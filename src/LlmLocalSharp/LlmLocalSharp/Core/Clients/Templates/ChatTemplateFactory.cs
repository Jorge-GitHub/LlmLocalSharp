using LlmLocalSharp.Core.Clients.Interfaces;
using LlmLocalSharp.Core.Entities.Constants.Enums;

namespace LlmLocalSharp.Core.Clients.Templates;

/// <summary>
/// Maps <see cref="ChatTemplateType"/> enum values to template instances.
/// </summary>
public static class ChatTemplateFactory
{
    /// <summary>Creates the chat template implementation for the requested template type.</summary>
    public static IChatTemplate Create(ChatTemplateType templateType)
    {
        return templateType switch
        {
            ChatTemplateType.Llama2 => new Llama2ChatTemplate(),
            ChatTemplateType.ChatMl => new ChatMlTemplate(),
            ChatTemplateType.Mistral => new MistralChatTemplate(),
            ChatTemplateType.None => new Llama2ChatTemplate(),
            _ => throw new ArgumentOutOfRangeException(nameof(templateType),
                $"Unsupported chat template type: {templateType}")
        };
    }
}
