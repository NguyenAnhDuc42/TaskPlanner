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
    // Appended at the end deliberately — Priority has no [HasConversion<string>()] in
    // ProjectTaskConfiguration, so EF Core persists it by ordinal. Inserting a new member
    // anywhere but the end would silently reinterpret every already-stored priority value.
    None,
}

