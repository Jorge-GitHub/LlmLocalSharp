using LlmLocalSharp.Core.Entities.Constants.Enums;

namespace LlmLocalSharp.Core.Entities.Clients.Messages.ContentParts;

public sealed class DocumentContentPart : ChatMessageContentPart
{
    public string Base64Data { get; }
    public DocumentMediaType MediaType { get; }

    /// <summary>Creates a document content part from base64-encoded document data.</summary>
    public DocumentContentPart(string base64Data, DocumentMediaType mediaType)
        : base(ContentPartType.Document)
    {
        this.Base64Data = base64Data ?? throw new ArgumentNullException(nameof(base64Data));
        this.MediaType = mediaType;
    }
}
