using System;
using Domain.Common;

namespace Domain.Entities.Support;

public class Tag : Entity
{
    public string Name { get; private set; } = null!;
    public string? Color { get; private set; }
    public Guid ProjectWorkspaceId { get; private set; }

    private Tag() { } // EF Core

    private Tag(Guid id, string name, string? color, Guid projectWorkspaceId)
        : base(id)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Tag name cannot be empty.", nameof(name));
        if (projectWorkspaceId == Guid.Empty) throw new ArgumentException("ProjectWorkspaceId cannot be empty.", nameof(projectWorkspaceId));

        Name = name.Trim();
        Color = string.IsNullOrWhiteSpace(color) ? null : color.Trim();
        ProjectWorkspaceId = projectWorkspaceId;
    }

    public static Tag Create(string name, string? color, Guid projectWorkspaceId)
        => new Tag(Guid.NewGuid(), name, color, projectWorkspaceId);

    public void Update(string name, string? color)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Tag name cannot be empty.", nameof(name));

        Name = name.Trim();
        Color = string.IsNullOrWhiteSpace(color) ? null : color.Trim();
        UpdateTimestamp();
    }
}
