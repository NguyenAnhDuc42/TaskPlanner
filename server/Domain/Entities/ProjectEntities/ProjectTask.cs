using System.ComponentModel.DataAnnotations;
using Domain.Common;
using Domain.Entities.ProjectEntities.ValueObject;
using Domain.Entities.Relationship;
using Domain.Enums;

namespace Domain.Entities.ProjectEntities;

public class ProjectTask : Entity
{
    public Guid ProjectListId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public Customization Customization { get; private set; } = Customization.CreateDefault();
    public Guid CreatorId { get; private set; }
    public Guid? StatusId { get; private set; }
    public bool IsArchived { get; private set; }
    public Priority Priority { get; private set; } = Priority.Low;
    public DateTimeOffset? StartDate { get; private set; }
    public DateTimeOffset? DueDate { get; private set; }
    public int? StoryPoints { get; private set; }
    public long? TimeEstimate { get; private set; }
    public long? OrderKey { get; private set; }

    // EF Core
    private ProjectTask() { }

    private ProjectTask(Guid id, Guid projectListId, string name, string? description, Customization customization, Guid creatorId, Guid? statusId, Priority priority, DateTimeOffset? startDate, DateTimeOffset? dueDate, int? storyPoints, long? timeEstimate, long orderKey)
    {
        Id = id;
        ProjectListId = projectListId;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = string.IsNullOrWhiteSpace(description) ? null : description;
        Customization = customization ?? Customization.CreateDefault();
        CreatorId = creatorId;
        StatusId = statusId;
        Priority = priority;
        StartDate = startDate;
        DueDate = dueDate;
        StoryPoints = storyPoints;
        OrderKey = orderKey;
        TimeEstimate = timeEstimate;
        IsArchived = false;
    }

    public static ProjectTask Create(Guid projectListId, string name, string? description, Customization? customization, Guid creatorId, Guid? statusId = null, Priority priority = Priority.Low, DateTimeOffset? startDate = null, DateTimeOffset? dueDate = null, int? storyPoints = null, long? timeEstimate = null, long orderKey = 10_000_000L)
        => new ProjectTask(Guid.NewGuid(), projectListId, name?.Trim() ?? throw new ArgumentNullException(nameof(name)), string.IsNullOrWhiteSpace(description) ? null : description?.Trim(), customization ?? Customization.CreateDefault(), creatorId, statusId, priority, startDate, dueDate, storyPoints, timeEstimate, orderKey);

    // Consolidated update: single method for name/description/schedule/priority/status/estimation/customization/orderKey
    public void Update(string? name = null, string? description = null, DateTimeOffset? startDate = null, DateTimeOffset? dueDate = null, Priority? priority = null, Guid? StatusId = null, int? storyPoints = null, long? timeEstimateSeconds = null, string? color = null, string? icon = null, long? orderKey = null)
    {
        var changed = false;

        var candidateName = name is null ? Name : (name.Trim() == string.Empty ? throw new ArgumentException("Name cannot be empty.", nameof(name)) : name.Trim());
        var candidateDescription = description is null ? Description : (string.IsNullOrWhiteSpace(description.Trim()) ? null : description.Trim());

        ValidateBasicInfo(candidateName, candidateDescription);
        ValidateSchedule(startDate ?? StartDate, dueDate ?? DueDate);
        ValidateEstimation(storyPoints ?? StoryPoints, timeEstimateSeconds ?? TimeEstimate);

        if (candidateName != Name) { Name = candidateName; changed = true; }
        if (candidateDescription != Description) { Description = candidateDescription; changed = true; }

        if (startDate != null || dueDate != null)
        {
            var finalStart = startDate ?? StartDate;
            var finalDue = dueDate ?? DueDate;
            if (finalStart != StartDate || finalDue != DueDate) { StartDate = finalStart; DueDate = finalDue; changed = true; }
        }

        if (priority.HasValue && priority.Value != Priority) { Priority = priority.Value; changed = true; }
        if (StatusId.HasValue && StatusId != this.StatusId) { this.StatusId = StatusId; changed = true; }

        if (storyPoints != null || timeEstimateSeconds != null)
        {
            if (storyPoints != StoryPoints || timeEstimateSeconds != TimeEstimate) { StoryPoints = storyPoints; TimeEstimate = timeEstimateSeconds; changed = true; }
        }

        if (color is not null || icon is not null)
        {
            var c = color?.Trim() ?? Customization.Color;
            var i = icon?.Trim() ?? Customization.Icon;
            var newCustomization = Customization.Create(c, i);
            if (!newCustomization.Equals(Customization)) { Customization = newCustomization; changed = true; }
        }

        if (orderKey.HasValue && orderKey != OrderKey) { OrderKey = orderKey.Value; changed = true; }

        if (changed) UpdateTimestamp();
    }

    public void UpdateDetails(string name, string? description)
    {
        var changed = false;
        var candidateName = name.Trim() == string.Empty ? throw new ArgumentException("Name cannot be empty.", nameof(name)) : name.Trim();
        var candidateDescription = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

        ValidateBasicInfo(candidateName, candidateDescription);

        if (candidateName != Name) { Name = candidateName; changed = true; }
        if (candidateDescription != Description) { Description = candidateDescription; changed = true; }

        if (changed) UpdateTimestamp();
    }

    public void UpdateStatus(Guid statusId)
    {
        if (StatusId != statusId)
        {
            StatusId = statusId;
            UpdateTimestamp();
        }
    }

    public void UpdatePriority(Priority priority)
    {
        if (Priority != priority)
        {
            Priority = priority;
            UpdateTimestamp();
        }
    }

    public void UpdateDates(DateTimeOffset? startDate, DateTimeOffset? dueDate)
    {
        ValidateSchedule(startDate, dueDate);
        var changed = false;
        if (StartDate != startDate) { StartDate = startDate; changed = true; }
        if (DueDate != dueDate) { DueDate = dueDate; changed = true; }
        if (changed) UpdateTimestamp();
    }

    public void UpdateEstimation(int? storyPoints, long? timeEstimate)
    {
        ValidateEstimation(storyPoints, timeEstimate);
        var changed = false;
        if (StoryPoints != storyPoints) { StoryPoints = storyPoints; changed = true; }
        if (TimeEstimate != timeEstimate) { TimeEstimate = timeEstimate; changed = true; }
        if (changed) UpdateTimestamp();
    }

    public void Archive() { if (IsArchived) return; IsArchived = true; UpdateTimestamp(); }
    public void Unarchive() { if (!IsArchived) return; IsArchived = false; UpdateTimestamp(); }

    private static void ValidateBasicInfo(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Task name cannot be empty.", nameof(name));
        if (name.Length > 200) throw new ArgumentException("Task name cannot exceed 200 characters.", nameof(name));
        if (description?.Length > 2000) throw new ArgumentException("Task description cannot exceed 2000 characters.", nameof(description));
    }

    private static void ValidateSchedule(DateTimeOffset? startDate, DateTimeOffset? dueDate)
    {
        if (startDate.HasValue && dueDate.HasValue && startDate > dueDate) throw new ArgumentException("Start date cannot be later than due date.", nameof(startDate));
    }

    private static void ValidateEstimation(int? storyPoints, long? timeEstimateSeconds)
    {
        if (storyPoints.HasValue && storyPoints < 0) throw new ArgumentOutOfRangeException(nameof(storyPoints));
        if (timeEstimateSeconds.HasValue && timeEstimateSeconds < 0) throw new ArgumentOutOfRangeException(nameof(timeEstimateSeconds));
    }
}
