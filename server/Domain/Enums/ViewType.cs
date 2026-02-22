using System.Text.Json.Serialization;

namespace Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ViewType
{
    List,
    Board,
    Dashboard,
    Doc
}
