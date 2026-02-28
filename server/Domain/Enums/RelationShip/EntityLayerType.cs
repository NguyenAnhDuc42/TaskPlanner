using System.Text.Json.Serialization;

namespace Domain.Enums.RelationShip;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EntityLayerType
{
    ProjectWorkspace,
    ProjectSpace,
    ProjectFolder,
    ProjectList,
    ProjectTask,
    ChatRoom
}
