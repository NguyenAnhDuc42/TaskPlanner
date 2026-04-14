using Domain.Common;
using Domain.Entities.ProjectEntities;
using Domain.Enums;
using Domain.Exceptions;

namespace Domain.Entities;

public class ProjectTask : TenantEntity
{
    public Guid? ProjectSpaceId { get; private set; }
    public Guid? ProjectFolderId { get; private set; }
    public string Name { get; private set; } = null!;
    public string Slug { get; private set; } = null!;
    public string? Description { get; private set; }
    public Customization Customization { get; private set; } = Customization.CreateDefault();
    public Guid? StatusId { get; private set; }
    public bool IsArchived { get; private set; }
    public Priority Priority { get; private set; } = Priority.Low;
    public DateTimeOffset? StartDate { get; private set; }
    public DateTimeOffset? DueDate { get; private set; }
    public int? StoryPoints { get; private set; }
    public long? TimeEstimate { get; private set; }
    public string? OrderKey { get; private set; }

    private readonly List<TaskAssignment> _assignees = new();
    public virtual IReadOnlyCollection<TaskAssignment> Assignees => _assignees.AsReadOnly();    

    // EF Core
    private ProjectTask() { }

    private ProjectTask(Guid id, Guid projectWorkspaceId, Guid? projectSpaceId, Guid? projectFolderId, string name, string slug, string? description, Customization customization, Guid creatorId, Guid? statusId, Priority priority, DateTimeOffset? startDate, DateTimeOffset? dueDate, int? storyPoints, long? timeEstimate, string orderKey)
        : base(id, projectWorkspaceId)
    {
        ProjectSpaceId = projectSpaceId;
        ProjectFolderId = projectFolderId;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Slug = slug ?? throw new ArgumentNullException(nameof(slug));
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

    public static ProjectTask Create(Guid projectWorkspaceId, Guid? projectSpaceId, Guid? projectFolderId, string name, string slug, string? description, Customization? customization, Guid creatorId, Guid? statusId = null, Priority priority = Priority.Low, DateTimeOffset? startDate = null, DateTimeOffset? dueDate = null, int? storyPoints = null, long? timeEstimate = null, string? orderKey = null)
        => new ProjectTask(Guid.NewGuid(), projectWorkspaceId, projectSpaceId, projectFolderId, name?.Trim() ?? throw new ArgumentNullException(nameof(name)), slug?.Trim().ToLowerInvariant() ?? throw new ArgumentNullException(nameof(slug)), string.IsNullOrWhiteSpace(description) ? null : description?.Trim(), customization ?? Customization.CreateDefault(), creatorId, statusId, priority, startDate, dueDate, storyPoints, timeEstimate, orderKey ?? FractionalIndex.Start());

    public void UpdateBasicInfo(string? name, string? slug, string? description)
    {
        EnsureNotArchived();

        var candidateName = name?.Trim() ?? Name;
        var candidateSlug = slug?.Trim().ToLowerInvariant() ?? Slug;
        var candidateDescription = description?.Trim() ?? Description;

        if (candidateName == Name && candidateSlug == Slug && candidateDescription == Description) return;

        ValidateBasicInfo(candidateName, candidateSlug, candidateDescription);

        Name = candidateName;
        Slug = candidateSlug;
        Description = string.IsNullOrWhiteSpace(candidateDescription) ? null : candidateDescription;

        UpdateTimestamp();
    }

    public void UpdateStatus(Guid statusId)
    {
        EnsureNotArchived();
        if (StatusId != statusId)
        {
            StatusId = statusId;
            UpdateTimestamp();
        }
    }

    public void UpdatePriority(Priority priority)
    {
        EnsureNotArchived();
        if (Priority != priority)
        {
            Priority = priority;
            UpdateTimestamp();
        }
    }

    public void UpdateDates(DateTimeOffset? startDate, DateTimeOffset? dueDate)
    {
        EnsureNotArchived();
        ValidateSchedule(startDate, dueDate);
        var changed = false;
        if (StartDate != startDate) { StartDate = startDate; changed = true; }
        if (DueDate != dueDate) { DueDate = dueDate; changed = true; }
        if (changed) UpdateTimestamp();
    }

    public void UpdateEstimation(int? storyPoints, long? timeEstimate)
    {
        EnsureNotArchived();
        ValidateEstimation(storyPoints, timeEstimate);
        var changed = false;
        if (StoryPoints != storyPoints) { StoryPoints = storyPoints; changed = true; }
        if (TimeEstimate != timeEstimate) { TimeEstimate = timeEstimate; changed = true; }
        if (changed) UpdateTimestamp();
    }
    public void AddAsignees(List<TaskAssignment> newAssignments)
    {
        EnsureNotArchived();
        foreach (var assignment in newAssignments)
        {
            if (!_assignees.Any(a => a.WorkspaceMemberId == assignment.WorkspaceMemberId))
            {
                _assignees.Add(assignment);
            }
        }
        if (newAssignments.Count > 0) UpdateTimestamp();
    }

    public void RemoveAsignees(List<Guid> memberIdsToRemove)
    {
        EnsureNotArchived();
        var removed = _assignees.RemoveAll(a => memberIdsToRemove.Contains(a.WorkspaceMemberId));
        if (removed > 0) UpdateTimestamp();
    }

    public void Archive() { if (IsArchived) return; IsArchived = true; UpdateTimestamp(); }
    public void Unarchive() { if (!IsArchived) return; IsArchived = false; UpdateTimestamp(); }


    private void EnsureNotArchived()
    {
        if (IsArchived) throw new BusinessRuleException("Cannot modify an archived task.");
    }

    private static void ValidateBasicInfo(string name, string slug, string? description)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new BusinessRuleException("Task name cannot be empty.");
        if (name.Length > 200) throw new BusinessRuleException("Task name cannot exceed 200 characters.");
        if (string.IsNullOrWhiteSpace(slug)) throw new BusinessRuleException("Slug cannot be empty.");
        if (description?.Length > 2000) throw new BusinessRuleException("Task description cannot exceed 2000 characters.");
    }

    private static void ValidateSchedule(DateTimeOffset? startDate, DateTimeOffset? dueDate)
    {
        if (startDate.HasValue && dueDate.HasValue && startDate > dueDate) 
            throw new BusinessRuleException("Start date cannot be later than due date.");
    }

    private static void ValidateEstimation(int? storyPoints, long? timeEstimateSeconds)
    {
        if (storyPoints.HasValue && storyPoints < 0) throw new BusinessRuleException("Story points cannot be negative.");
        if (timeEstimateSeconds.HasValue && timeEstimateSeconds < 0) throw new BusinessRuleException("Time estimate cannot be negative.");
    }
}
