using System.Text.Json.Serialization;

namespace Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum StatusCategory
{
    NotStarted,
    Active,
    Done,
    Closed
}
