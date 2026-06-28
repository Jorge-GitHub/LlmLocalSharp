namespace LlmLocalSharp.Core.Entities.Constants.Enums;

public enum ContentPartType
{
    Text = 0,
    Image = 1,
    Document = 2,
}

public enum ImageSourceType
{
    Url = 0,
    Base64 = 1,
}

public enum ImageMediaType
{
    Jpeg = 0,
    Png = 1,
    Gif = 2,
    Webp = 3,
}

public enum DocumentMediaType
{
    Pdf = 0,
    PlainText = 1,
}
