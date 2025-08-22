using Domain.Common;
using Domain.Entities.ProjectWorkspace;
using System;
using System.Collections.Generic;

namespace Domain.Entities.Support;

public class Tag : Entity
{
    public string Name { get; private set; } = null!;
    public string Color { get; private set; } = null!;

    public ICollection<ProjectTask> Tasks { get; set; } = new List<ProjectTask>();

    private Tag() { } // For EF Core

    private Tag(Guid id, string name, string color, Guid projectWorkspaceId)
    {
        Id = id;
        Name = name;
        Color = color;
    }

    public static Tag Create(string name, string color, Guid projectWorkspaceId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tag name cannot be empty.", nameof(name));

        return new Tag(Guid.NewGuid(), name, color, projectWorkspaceId);
    }
}