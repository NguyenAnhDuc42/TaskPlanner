using Domain.Common;
using System;

namespace Domain.Entities.Support;

public class Status : Entity
{
    public Guid ProjectWorkspaceId { get; private set; }
    public Guid? ProjectSpaceId { get; set; }
    public string Name { get; private set; } = null!;
    public string Color { get; private set; } = null!;
    public int OrderIndex { get; private set; }
    public bool IsDefaultStatus { get; private set; }

    private Status() { } // For EF Core

    private Status(Guid id, string name, string color, int orderIndex, Guid projectWorkspaceId)
    {
        Id = id;
        Name = name;
        Color = color;
        OrderIndex = orderIndex;
        ProjectWorkspaceId = projectWorkspaceId;
        IsDefaultStatus = false; // Initialize IsDefaultStatus
        ProjectSpaceId = null; // Initialize ProjectSpaceId
    }

    public static Status Create(string name, string color, int orderIndex, Guid projectWorkspaceId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Status name cannot be empty.", nameof(name));

        return new Status(Guid.NewGuid(), name, color, orderIndex, projectWorkspaceId);
    }

    public void UpdateDetails(string newName, string newColor)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Status name cannot be empty.", nameof(newName));
        if (Name == newName && Color == newColor) return;

        Name = newName;
        Color = newColor;
    }
    public void UpdateOrderIndex(int newIndex)
    {
        if (newIndex < 0) throw new ArgumentOutOfRangeException(nameof(newIndex), "Order index cannot be negative.");
        if (OrderIndex == newIndex) return;

        OrderIndex = newIndex;
    }
}