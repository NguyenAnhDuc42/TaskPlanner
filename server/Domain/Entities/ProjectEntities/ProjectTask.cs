using System.ComponentModel.DataAnnotations;
using Domain.Common.Interfaces;
using Domain.Entities.Relationship;
using Domain.Entities.Support;
using Domain.Enums;

namespace Domain.Entities.ProjectEntities;

public class ProjectTask : Aggregate
{
    [Required] public Guid ProjectListId { get; private set; }

    [Required] public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    [Required] public Guid CreatorId { get; private set; }

    public Guid? StatusId { get; private set; }
    public bool IsArchived { get; private set; }
    public Priority Priority { get; private set; }
    public DateTimeOffset? StartDate { get; private set; }
    public DateTimeOffset? DueDate { get; private set; }
    public int? StoryPoints { get; private set; }
    public long? TimeEstimate { get; private set; }
    public long? OrderKey { get; private set; }

        private ProjectTask() { } // EF Core

    internal ProjectTask(Guid id, Guid projectListId, string name, string? description,
        Priority priority, DateTimeOffset? startDate, DateTimeOffset? dueDate,
        long orderKey, Guid creatorId)
    {
        Id = id;
        ProjectListId = projectListId;
        Name = name;
        Description = description;
        Priority = priority;
        StartDate = startDate;
        DueDate = dueDate;
        OrderKey = orderKey;
        CreatorId = creatorId;
        IsArchived = false;
        StatusId = null;
        StoryPoints = null;
        TimeEstimate = null;
    }

    // === SELF MANAGEMENT ===

    public void UpdateBasicInfo(string name, string? description)
    {
        name = name?.Trim() ?? string.Empty;
        description = string.IsNullOrWhiteSpace(description?.Trim()) ? null : description.Trim();

        if (Name == name && Description == description) return;

        ValidateBasicInfo(name, description);

        Name = name;
        Description = description;
        UpdateTimestamp();
    }

    public void UpdateSchedule(DateTimeOffset? startDate, DateTimeOffset? dueDate)
    {
        if (StartDate == startDate && DueDate == dueDate) return;

        ValidateSchedule(startDate, dueDate);

        StartDate = startDate;
        DueDate = dueDate;
        UpdateTimestamp();
    }

    public void ChangePriority(Priority newPriority)
    {
        if (Priority == newPriority) return;

        var oldPriority = Priority;
        Priority = newPriority;
        UpdateTimestamp();
    }
    public void UpdateStatus(Guid statusId, bool isCompletedStatus)
    {
        if (StatusId == statusId) return;

        StatusId = statusId;

        UpdateTimestamp();

    }

    public void SetEstimation(int? storyPoints, long? timeEstimateSeconds)
    {
        ValidateEstimation(storyPoints, timeEstimateSeconds);

        if (StoryPoints == storyPoints && TimeEstimate == timeEstimateSeconds) return;

        var oldStoryPoints = StoryPoints;
        var oldTimeEstimate = TimeEstimate;
        StoryPoints = storyPoints;
        TimeEstimate = timeEstimateSeconds;
        UpdateTimestamp();
    }

    internal void UpdateOrderKey(long newOrderKey)
    {
        if (OrderKey == newOrderKey) return;

        OrderKey = newOrderKey;
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

    private static void ValidateBasicInfo(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Task name cannot be empty.", nameof(name));
        if (name.Length > 200)
            throw new ArgumentException("Task name cannot exceed 200 characters.", nameof(name));
        if (description?.Length > 2000)
            throw new ArgumentException("Task description cannot exceed 2000 characters.", nameof(description));
    }

    private static void ValidateSchedule(DateTimeOffset? startDate, DateTimeOffset? dueDate)
    {
        if (startDate.HasValue && dueDate.HasValue && startDate > dueDate)
            throw new ArgumentException("Start date cannot be later than due date.", nameof(startDate));
    }

    private static void ValidateEstimation(int? storyPoints, long? timeEstimateSeconds)
    {
        if (storyPoints.HasValue && storyPoints < 0) throw new ArgumentOutOfRangeException(nameof(storyPoints));
        if (timeEstimateSeconds.HasValue && timeEstimateSeconds < 0) throw new ArgumentOutOfRangeException(nameof(timeEstimateSeconds));
    }
}