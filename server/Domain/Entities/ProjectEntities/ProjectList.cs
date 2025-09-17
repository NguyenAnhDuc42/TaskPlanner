
using System.ComponentModel.DataAnnotations;
using Domain.Entities.Relationship;
using Domain.Enums;

namespace Domain.Entities.ProjectEntities;

public class ProjectList : Aggregate
{
    [Required] public Guid ProjectSpaceId { get; private set; }
    public Guid? ProjectFolderId { get; private set; }
    [Required] public string Name { get; private set; } = null!;
    public string Color { get; private set; } = "#cdcbcbff";
    public string Icon { get; private set; } = null!;
    public long? OrderKey { get; private set; }
    public Visibility Visibility { get; private set; }
    public bool IsArchived { get; private set; }
    [Required] public Guid CreatorId { get; private set; }
    public DateTimeOffset? StartDate { get; private set; }
    public DateTimeOffset? DueDate { get; private set; }

    private ProjectList() { } // For EF Core

    internal ProjectList(Guid id, Guid projectSpaceId, Guid? projectFolderId, string name, Visibility visibility, long orderKey, Guid creatorId)
    {
        Id = id;
        ProjectSpaceId = projectSpaceId;
        ProjectFolderId = projectFolderId;
        Name = name;
        Visibility = visibility;
        OrderKey = orderKey;
        CreatorId = creatorId;
        IsArchived = false;
    }

    // === SELF MANAGEMENT ===

    public void UpdateBasicInfo(string name, string? description)
    {
        name = name?.Trim() ?? string.Empty;
        description = string.IsNullOrWhiteSpace(description?.Trim()) ? null : description.Trim();

        if (Name == name) return;

        ValidateBasicInfo(name, description);

        var oldName = Name;
        Name = name;
        UpdateTimestamp();
    }

    public void ChangeVisibility(Visibility newVisibility)
    {
        if (Visibility == newVisibility) return;

        var oldVisibility = Visibility;
        Visibility = newVisibility;
        UpdateTimestamp();
    }

    public void SetDateRange(DateTimeOffset? startDate, DateTimeOffset? dueDate)
    {
        if (startDate.HasValue && dueDate.HasValue && startDate > dueDate)
            throw new ArgumentException("Start date cannot be later than due date.", nameof(startDate));

        if (StartDate == startDate && DueDate == dueDate) return;

        var oldStartDate = StartDate;
        var oldDueDate = DueDate;
        StartDate = startDate;
        DueDate = dueDate;
        UpdateTimestamp();
    }

    public void Archive()
    {
        if (IsArchived) return;

        IsArchived = true;
        UpdateTimestamp();
    }

    public void Unarchive()
    {
        if (!IsArchived) return;

        IsArchived = false;
        UpdateTimestamp();
    }

    internal void UpdateOrderKey(long newOrderKey)
    {
        if (OrderKey == newOrderKey) return;

        OrderKey = newOrderKey;
        UpdateTimestamp();
    }

    internal void MoveToFolder(Guid? newFolderId)
    {
        if (ProjectFolderId == newFolderId) return;

        var oldFolderId = ProjectFolderId;
        ProjectFolderId = newFolderId;
        UpdateTimestamp();
    }

    internal void MoveToSpace(Guid newSpaceId)
    {
        if (ProjectSpaceId == newSpaceId) return;

        var oldSpaceId = ProjectSpaceId;
        ProjectSpaceId = newSpaceId;
        UpdateTimestamp();
    }


    // === VALIDATION HELPERS ===

    private static void ValidateBasicInfo(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("List name cannot be empty.", nameof(name));
        if (name.Length > 100)
            throw new ArgumentException("List name cannot exceed 100 characters.", nameof(name));
        if (description?.Length > 500)
            throw new ArgumentException("List description cannot exceed 500 characters.", nameof(description));
    }

}