using Domain.Entities.ProjectEntities;
using System;
using System.Collections.Generic;

namespace Domain.Entities.Support;

public class Tag
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string Color { get; private set; } = null!;

    // No direct ICollection<ProjectTask> Tasks here, will use join entity

    private Tag() { } // For EF Core

    private Tag(Guid id, string name, string color)
    {
        Id = id;
        Name = name;
        Color = color;
    }

    public static Tag Create(string name, string color)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tag name cannot be empty.", nameof(name));

        return new Tag(Guid.NewGuid(), name, color);
    }
}
