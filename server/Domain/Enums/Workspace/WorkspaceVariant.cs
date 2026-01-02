using System.Text.Json.Serialization;

namespace Domain.Enums.Workspace;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum WorkspaceVariant
{
    Personal,
    Team,
    Company,

}
