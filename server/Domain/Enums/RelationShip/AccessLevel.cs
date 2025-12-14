using System.Text.Json.Serialization;

namespace Domain.Enums.RelationShip;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AccessLevel
{
    Manager,
    Editor,
    Viewer
}
