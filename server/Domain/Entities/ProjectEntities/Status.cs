using System;
using Domain.Common;
using Domain.Enums;
using Domain.Enums.RelationShip;

namespace Domain.Entities.ProjectEntities;

public class Status : Entity
{
    public Guid WorkflowId { get; private set; }
    public string Name { get; private set; } = null!;
    public string Color { get; private set; } = null!;
    public StatusCategory Category { get; private set; }
    public bool IsDefaultStatus { get; private set; }

    private Status() { } // EF Core

    private Status(Guid id, Guid workflowId, string name, string color, StatusCategory category, Guid creatorId)
        : base(id)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Status name cannot be empty.", nameof(name));
        if (string.IsNullOrWhiteSpace(color)) throw new ArgumentException("Status color cannot be empty.", nameof(color));
        if (workflowId == Guid.Empty) throw new ArgumentException(nameof(workflowId));

        Name = name.Trim();
        Color = color.Trim();
        Category = category;
        WorkflowId = workflowId;
        IsDefaultStatus = false;
        CreatorId = creatorId;
    }

    public static Status Create(Guid workflowId, string name, string color, StatusCategory category, Guid creatorId)
        => new Status(Guid.NewGuid(), workflowId, name, color, category, creatorId);

    public static List<Status> CreateDefaultStatuses(Guid workflowId, Guid creatorId)
    {
        return new List<Status>
        {
            new Status(Guid.NewGuid(), workflowId, "To Do", "#808080", StatusCategory.NotStarted, creatorId),
            new Status(Guid.NewGuid(), workflowId, "In Progress", "#1e90ff", StatusCategory.Active, creatorId),
            new Status(Guid.NewGuid(), workflowId, "Complete", "#008000", StatusCategory.Done, creatorId)
        };
    }

    public void UpdateDetails(string newName, string newColor, StatusCategory? newCategory = null)
    {
        if (string.IsNullOrWhiteSpace(newName)) throw new ArgumentException("Status name cannot be empty.", nameof(newName));
        if (string.IsNullOrWhiteSpace(newColor)) throw new ArgumentException("Status color cannot be empty.", nameof(newColor));

        var changed = false;
        if (Name != newName.Trim()) { Name = newName.Trim(); changed = true; }
        if (Color != newColor.Trim()) { Color = newColor.Trim(); changed = true; }
        if (newCategory.HasValue && Category != newCategory.Value) { Category = newCategory.Value; changed = true; }

        if (changed) UpdateTimestamp();
    }

    public void SetDefault(bool isDefault)
    {
        if (IsDefaultStatus == isDefault) return;
        IsDefaultStatus = isDefault;
        UpdateTimestamp();
    }

    public void SetWorkflow(Guid workflowId)
    {
        if (WorkflowId == workflowId) return;
        WorkflowId = workflowId;
        UpdateTimestamp();
    }
}
