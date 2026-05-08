using System.Text.Json.Serialization;

namespace Domain.Entities;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BlockType 
{
    Paragraph,
    Heading1,
    Heading2,
    Heading3,
    BulletList,
    OrderedList,
    TaskItem,
    Image,
    File,
    Video,
    CodeBlock,
    Divider,
    Quote
}
