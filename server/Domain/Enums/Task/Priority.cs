using System.Text.Json.Serialization;

namespace Domain;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Priority
{
    None,
    Low,
    Normal,
    Medium = Normal,
    High,
    Urgent,
}

