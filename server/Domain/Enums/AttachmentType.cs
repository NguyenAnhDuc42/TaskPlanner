using System.Text.Json.Serialization;

namespace Domain;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AttachmentType
{
    None,
    File,
    Link,
    Media,
    Embed
}


