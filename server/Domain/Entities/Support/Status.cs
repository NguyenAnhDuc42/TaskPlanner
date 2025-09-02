using System;
using Domain.Common;

namespace Domain.Entities.Support;

public class Status : Entity
{
    public Guid ProjectWorkspaceId { get; private set; }
    public Guid? ProjectSpaceId { get; private set; }
    public string Name { get; private set; } = null!;
    public string Color { get; private set; } = null!;
    public int OrderIndex { get; private set; }
    public bool IsDefaultStatus { get; private set; }

    private Status() { } // EF Core

    private Status(Guid id, string name, string color, int orderIndex, Guid projectWorkspaceId)
        : base(id)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Status name cannot be empty.", nameof(name));
        if (string.IsNullOrWhiteSpace(color)) throw new ArgumentException("Status color cannot be empty.", nameof(color));
        if (projectWorkspaceId == Guid.Empty) throw new ArgumentException("ProjectWorkspaceId cannot be empty.", nameof(projectWorkspaceId));

        Name = name.Trim();
        Color = color.Trim();
        OrderIndex = orderIndex;
        ProjectWorkspaceId = projectWorkspaceId;
        ProjectSpaceId = null;
        IsDefaultStatus = false;
    }

    public static Status Create(string name, string color, int orderIndex, Guid projectWorkspaceId)
        => new Status(Guid.NewGuid(), name, color, orderIndex, projectWorkspaceId);

    public void UpdateDetails(string newName, string newColor)
    {
        
        if (string.IsNullOrWhiteSpace(newName)) throw new ArgumentException("Status name cannot be empty.", nameof(newName));
        if (string.IsNullOrWhiteSpace(newColor)) throw new ArgumentException("Status color cannot be empty.", nameof(newColor));
        if (Name == newName.Trim() && Color == newColor.Trim()) return;

        Name = newName.Trim();
        Color = newColor.Trim();
        UpdateTimestamp();
    }

    public void UpdateOrderIndex(int newIndex)
    {
        if (newIndex < 0) throw new ArgumentOutOfRangeException(nameof(newIndex));
        if (OrderIndex == newIndex) return;

        OrderIndex = newIndex;
        UpdateTimestamp();
    }

    public void SetDefault(bool isDefault)
    {
        if (IsDefaultStatus == isDefault) return;
        IsDefaultStatus = isDefault;
        UpdateTimestamp();
    }

    public void AssignToSpace(Guid spaceId)
    {
        if (spaceId == Guid.Empty) throw new ArgumentException(nameof(spaceId));
        ProjectSpaceId = spaceId;
        UpdateTimestamp();
    }
}
