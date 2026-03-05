using System.Text.Json.Serialization;

namespace Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Priority
{
    Urgent,
    High,
    Normal,
    Low,
    Medium = Normal,
}
