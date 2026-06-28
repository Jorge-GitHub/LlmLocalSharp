using LlmLocalSharp.Core.Entities.Constants.Enums;

namespace LlmLocalSharp.Core.Entities.Clients.Messages.ContentParts;

/// <summary>
/// Base class for a single content block inside a multi-modal chat message.
/// Use the static factory methods to create instances.
/// </summary>
public abstract class ChatMessageContentPart
{
    public ContentPartType Type { get; }

    /// <summary>Initializes a content part with its type.</summary>
    protected ChatMessageContentPart(ContentPartType type) => this.Type = type;

    /// <summary>Creates a text content part.</summary>
    public static TextContentPart FromText(string text) => new(text);

    /// <summary>Creates an image content part that references an image URL.</summary>
    public static ImageContentPart FromImageUrl(string url)
        => new(ImageSourceType.Url, url, null);

    /// <summary>Creates an image content part from base64-encoded image data.</summary>
    public static ImageContentPart FromImageBase64(string base64Data, ImageMediaType mediaType)
        => new(ImageSourceType.Base64, base64Data, mediaType);

    /// <summary>Creates a document content part from base64-encoded document data.</summary>
    public static DocumentContentPart FromDocumentBase64(string base64Data, DocumentMediaType mediaType)
        => new(base64Data, mediaType);
}
