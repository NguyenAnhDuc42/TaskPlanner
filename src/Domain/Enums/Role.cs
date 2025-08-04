using System.Text.Json.Serialization;

namespace src.Domain.Enums;
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Role
{
    Owner ,
    Admin ,
    Member ,
    Guest 
}
