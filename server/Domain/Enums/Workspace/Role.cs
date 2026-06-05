using System.Text.Json.Serialization;

namespace Domain;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Role
{
    Guest = 1,
    Member = 2,
    Admin = 3,
    Owner = 4,
}

public static class RoleExtensions
{
    public static bool IsAtLeast(this Role role, Role requiredRole)
    {
        return role >= requiredRole;
    }
}

