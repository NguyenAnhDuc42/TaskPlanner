using System;
using Domain.Common;

namespace Domain.Entities.Support;

public class Checklist : Entity
{
    public Guid ProjectTaskId { get; private set; }
    public string Title { get; private set; } = null!;
    public int OrderIndex { get; private set; }

    private Checklist() { } // EF Core

    private Checklist(Guid id, string title, Guid projectTaskId, int orderIndex = 0)
        : base(id)
    {
        if (string.IsNullOrWhiteSpace(title)) 
            throw new ArgumentException("Checklist title cannot be empty.", nameof(title));
        if (projectTaskId == Guid.Empty) 
            throw new ArgumentException("ProjectTaskId cannot be empty.", nameof(projectTaskId));

        Title = title.Trim();
        ProjectTaskId = projectTaskId;
        OrderIndex = orderIndex;
    }

    public static Checklist Create(string title, Guid projectTaskId, int orderIndex = 0)
        => new Checklist(Guid.NewGuid(), title, projectTaskId, orderIndex);

    public void UpdateTitle(string newTitle)
    {
        if (string.IsNullOrWhiteSpace(newTitle)) 
            throw new ArgumentException("Checklist title cannot be empty.", nameof(newTitle));
        if (Title == newTitle.Trim()) return;

        Title = newTitle.Trim();
        UpdateTimestamp();
    }

    public void UpdateOrder(int newIndex)
    {
        if (newIndex < 0) throw new ArgumentOutOfRangeException(nameof(newIndex));
        if (OrderIndex == newIndex) return;

        OrderIndex = newIndex;
        UpdateTimestamp();
    }
}
