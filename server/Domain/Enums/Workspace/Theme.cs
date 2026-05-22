using System.Text.Json.Serialization;
namespace Domain;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Theme
{
    Dark,
    Mars,
    DeepSpace,
    Boreal
}
