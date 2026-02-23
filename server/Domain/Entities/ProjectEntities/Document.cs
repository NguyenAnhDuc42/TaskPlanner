using Domain.Common;
using Domain.Enums.RelationShip;

namespace Domain.Entities.ProjectEntities;

public class Document : Entity
{
    public Guid LayerId { get; private set; }
    public EntityLayerType LayerType { get; private set; }
    public string Name { get; private set; } = null!;
    public string Content { get; private set; } = string.Empty;

    private Document() { }

    private Document(Guid layerId, EntityLayerType layerType, string name, string content, Guid creatorId)
    {
        LayerId = layerId;
        LayerType = layerType;
        Name = name;
        Content = content;
        CreatorId = creatorId;
    }

    public static Document Create(Guid layerId, EntityLayerType layerType, string name, string content, Guid creatorId)
    {
        return new Document(layerId, layerType, name, content, creatorId);
    }

    public void Update(string name, string content)
    {
        Name = name;
        Content = content;
        UpdateTimestamp();
    }
}
