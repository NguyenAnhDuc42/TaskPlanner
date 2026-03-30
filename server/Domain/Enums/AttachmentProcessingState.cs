using System.Text.Json.Serialization;

namespace Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AttachmentProcessingState
{
    Uploaded,
    Scanning,
    Ready,
    Failed,
    Deleted
}
