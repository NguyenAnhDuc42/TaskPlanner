using System.Text.Json.Serialization;

namespace Domain;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AccessLevel
{
    None,
    Manager,
    Editor,
    Viewer
}

