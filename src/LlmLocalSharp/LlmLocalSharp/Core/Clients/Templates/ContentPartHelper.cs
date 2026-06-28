using LlmLocalSharp.Core.Entities.Clients.Messages.ContentParts;
using System.Text;

namespace LlmLocalSharp.Core.Clients.Templates;

internal static class ContentPartHelper
{
    /// <summary>Extracts text content from a list of chat message content parts.</summary>
    internal static string ExtractText(IReadOnlyList<ChatMessageContentPart> contentParts)
    {
        StringBuilder sb = new();

        foreach (ChatMessageContentPart part in contentParts)
        {
            if (part is TextContentPart text)
            {
                sb.Append(text.Text);
            }
        }

        return sb.ToString();
    }
}
