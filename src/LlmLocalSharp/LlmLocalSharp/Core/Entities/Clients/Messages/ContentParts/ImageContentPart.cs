using LlmLocalSharp.Core.Entities.Constants.Enums;

namespace LlmLocalSharp.Core.Entities.Clients.Messages.ContentParts;

public sealed class ImageContentPart : ChatMessageContentPart
{
    public ImageSourceType SourceType { get; }
    public string Data { get; }
    public ImageMediaType? MediaType { get; }

    /// <summary>Creates an image content part from a URL or base64 payload.</summary>
    public ImageContentPart(ImageSourceType sourceType, string data, ImageMediaType? mediaType)
        : base(ContentPartType.Image)
    {
        this.SourceType = sourceType;
        this.Data = data ?? throw new ArgumentNullException(nameof(data));
        this.MediaType = mediaType;
    }
}
