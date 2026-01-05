using System.Text.Json.Serialization;
namespace Domain.Enums.Workspace;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Theme
{
    Light,
    Dark,
    System
}