using System.Text.Json.Serialization;
namespace Domain.Enums.Workspace;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Theme
{
    Dark,
    Mars,
    DeepSpace,
    Boreal
}