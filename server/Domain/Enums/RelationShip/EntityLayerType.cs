using System.Text.Json.Serialization;

namespace Domain;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EntityLayerType
{
    ProjectWorkspace,
    ProjectSpace,
    ProjectFolder,
    ProjectTask
}

