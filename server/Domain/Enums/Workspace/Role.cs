using System.Text.Json.Serialization;

namespace Domain;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Role
{
    Owner,
    Admin,
    Member,
    Guest,
}

