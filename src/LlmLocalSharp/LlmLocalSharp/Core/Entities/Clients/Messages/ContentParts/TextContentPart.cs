using LlmLocalSharp.Core.Entities.Constants.Enums;

namespace LlmLocalSharp.Core.Entities.Clients.Messages.ContentParts;

public sealed class TextContentPart : ChatMessageContentPart
{
    public string Text { get; }

    /// <summary>Creates a text content part.</summary>
    public TextContentPart(string text) : base(ContentPartType.Text)
    {
        this.Text = text ?? throw new ArgumentNullException(nameof(text));
    }
}
