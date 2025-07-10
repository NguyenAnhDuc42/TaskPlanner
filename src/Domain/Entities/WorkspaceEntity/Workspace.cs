using System;
using src.Domain.Entities.WorkspaceEntity.Relationships;

namespace src.Domain.Entities.WorkspaceEntity;

public class Workspace : Agregate<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Color { get; private set; } = string.Empty;
    public string Icon { get; private set; } = string.Empty;
    public bool IsPrivate { get; private set; }

    public ICollection<Space>? Spaces { get; set; } = new List<Space>();
    public ICollection<UserWorkspace>? Members { get; set; } = new List<UserWorkspace>();

    public Guid CreatorId { get; private set; }
    public string CreatorName { get; private set; } = string.Empty;


    private Workspace() { }
    private Workspace(Guid id, string name, string description, string color, string icon, Guid creatorId, string creatorName, bool isPrivate) : base(id)
    {
        Name = name;
        Description = description;
        Color = color;
        Icon = icon;
        CreatorId = creatorId;
        CreatorName = creatorName;
        IsPrivate = isPrivate;
    }

    public static Workspace Create(string name, string description, string color, string icon, Guid creatorId, string creatorName, bool isPrivate)
    {
        var id = Guid.NewGuid();
        return new Workspace(id, name, description, color, icon, creatorId, creatorName, isPrivate);
    }

}

