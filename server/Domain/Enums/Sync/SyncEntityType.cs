using System.Text.Json.Serialization;

namespace Domain;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SyncEntityType
{
    Workspace,
    Space,
    Folder,
    Task,
    Status,
    Comment,
    Document,
    DocumentBlock,
    Member,
    EntityAccess
}
