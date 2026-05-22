using System.Text.Json.Serialization;

namespace Domain;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Priority
{
    Urgent,
    High,
    Normal,
    Low,
    Medium = Normal,
}

