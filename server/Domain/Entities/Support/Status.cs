using Domain.Common;
using System;

namespace Domain.Entities.Support;

public class Status : Entity
{
    public string Name { get; private set; } = null!;
    public string Color { get; private set; } = null!;
    public int OrderIndex { get; private set; }
    public Guid ProjectWorkspaceId { get; private set; }
    public bool IsDoneStatus { get; private set; }

    private Status() { } // For EF Core

    private Status(Guid id, string name, string color, int orderIndex, Guid projectWorkspaceId, bool isDoneStatus)
    {
        Id = id;
        Name = name;
        Color = color;
        OrderIndex = orderIndex;
        ProjectWorkspaceId = projectWorkspaceId;
        IsDoneStatus = isDoneStatus;
    }

    public static Status Create(string name, string color, int orderIndex, Guid projectWorkspaceId, bool isDoneStatus)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Status name cannot be empty.", nameof(name));

        return new Status(Guid.NewGuid(), name, color, orderIndex, projectWorkspaceId, isDoneStatus);
    }
}