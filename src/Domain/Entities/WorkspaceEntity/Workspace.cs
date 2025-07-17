using System;
using src.Domain.Entities.WorkspaceEntity.Relationships;

using src.Domain.Enums;

namespace src.Domain.Entities.WorkspaceEntity;

public class Workspace : Agregate<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Color { get; private set; } = string.Empty;
    public bool IsPrivate { get; private set; }

    public ICollection<Space> Spaces { get; set; } = new List<Space>();
    public ICollection<UserWorkspace> Members { get; set; } = new List<UserWorkspace>();

    public Guid CreatorId { get; private set; }

    private Workspace() { }
    private Workspace(Guid id, string name, string description, string color, Guid creatorId, bool isPrivate) : base(id)
    {
        Name = name;
        Description = description;
        Color = color;
        CreatorId = creatorId;
        IsPrivate = isPrivate;
    }

    public static Workspace Create(string name, string description, string color, Guid creatorId, bool isPrivate)
    {
        var workspace = new Workspace(Guid.NewGuid(), name, description, color, creatorId, isPrivate);
        var ownerMembership = new UserWorkspace(creatorId, workspace.Id, Role.Owner);
        workspace.Members.Add(ownerMembership);

        return workspace;
    }

    public void CreateSpace(Space space)
    {
        Spaces.Add(space);
    }

}
