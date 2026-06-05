using System.Text.Json.Serialization;

namespace Domain;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AccessLevel
{
    None = 0,
    Viewer = 1,
    Editor = 2,
    Manager = 3,
}

public static class AccessLevelExtensions
{
    public static bool IsAtLeast(this AccessLevel access, AccessLevel requiredAccess)
    {
        return access >= requiredAccess;
    }
}

