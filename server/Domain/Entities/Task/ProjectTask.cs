using Domain.Common;
using Domain.Enums;
using Domain.Exceptions;

namespace Domain.Entities;

public class ProjectTask : TenantEntity
{
    public Guid? ProjectSpaceId { get; private set; }
    public Guid? ProjectFolderId { get; private set; }
    public string Name { get; private set; } = null!;
    public string Slug { get; private set; } = null!;
    public Guid DefaultDocumentId { get; private set; }
    public string Color { get; private set; } = "#FFFFFF";
    public string? Icon { get; private set; }
    public Guid? StatusId { get; private set; }
    public bool IsArchived { get; private set; }
    public Priority Priority { get; private set; } = Priority.Low;
    public DateTimeOffset? StartDate { get; private set; }
    public DateTimeOffset? DueDate { get; private set; }
    public int? StoryPoints { get; private set; }
    public long? TimeEstimateSeconds { get; private set; }
    public string OrderKey { get; private set; } = null!;

    private readonly List<TaskAssignment> _assignees = new();
    public virtual IReadOnlyCollection<TaskAssignment> Assignees => _assignees.AsReadOnly();    

    // EF Core
    private ProjectTask() { }

    private ProjectTask(Guid id, Guid projectWorkspaceId, Guid? projectSpaceId, Guid? projectFolderId, string name, string slug, Guid defaultDocumentId, string color, string? icon, Guid creatorId, Guid? statusId, Priority priority, DateTimeOffset? startDate, DateTimeOffset? dueDate, int? storyPoints, long? timeEstimateSeconds, string orderKey)
        : base(id, projectWorkspaceId)
    {
        ProjectSpaceId = projectSpaceId;
        ProjectFolderId = projectFolderId;
        Name = name;
        Slug = slug;
        DefaultDocumentId = defaultDocumentId;
        Color = color;
        Icon = icon;
        StatusId = statusId;
        Priority = priority;
        StartDate = startDate;
        DueDate = dueDate;
        StoryPoints = storyPoints;
        TimeEstimateSeconds = timeEstimateSeconds;
        OrderKey = orderKey;
        IsArchived = false;

        // Audit is initialized in base constructor
        InitializeAudit(creatorId);
    }

    public static ProjectTask Create(Guid projectWorkspaceId, Guid? projectSpaceId, Guid? projectFolderId, string name, string slug, Guid defaultDocumentId, string? color, string? icon, Guid creatorId, Guid? statusId = null, Priority priority = Priority.Low, DateTimeOffset? startDate = null, DateTimeOffset? dueDate = null, int? storyPoints = null, long? timeEstimateSeconds = null, string? orderKey = null)
    {
        return new ProjectTask(
            Guid.NewGuid(), 
            projectWorkspaceId, 
            projectSpaceId, 
            projectFolderId, 
            name, 
            slug, 
            defaultDocumentId, 
            color ?? "#FFFFFF", 
            icon,
            creatorId, 
            statusId, 
            priority, 
            startDate, 
            dueDate, 
            storyPoints, 
            timeEstimateSeconds, 
            orderKey ?? FractionalIndex.Start());
    }

    public static List<ProjectTask> CreateDefaults(Guid projectWorkspaceId, Guid spaceId, Guid folderId, Guid statusId, Guid creatorId, Guid exploreDocId, Guid standaloneDocId)
    {
        var start = FractionalIndex.Start();
        return new List<ProjectTask>
        {
            Create(
                projectWorkspaceId: projectWorkspaceId,
                projectSpaceId: spaceId,
                projectFolderId: folderId,
                name: "Explore the hierarchy",
                slug: "explore-hierarchy",
                defaultDocumentId: exploreDocId,
                color: null,
                icon: null,
                creatorId: creatorId,
                statusId: statusId,
                orderKey: start
            ),
            Create(
                projectWorkspaceId: projectWorkspaceId,
                projectSpaceId: spaceId,
                projectFolderId: null,
                name: "Standalone Task",
                slug: "standalone-task",
                defaultDocumentId: standaloneDocId,
                color: null,
                icon: null,
                creatorId: creatorId,
                statusId: statusId,
                orderKey: FractionalIndex.After(start)
            )
        };
    }

    public void UpdateName(string name)
    {
        EnsureNotArchived();
        Name = name;
        UpdateTimestamp();
    }

    public void UpdateSlug(string slug)
    {
        EnsureNotArchived();
        if (Slug == slug) return;
        Slug = slug;
        UpdateTimestamp();
    }

    public void UpdateColor(string color)
    {
        EnsureNotArchived();
        if (Color == color) return;
        Color = color;
        UpdateTimestamp();
    }

    public void UpdateIcon(string? icon)
    {
        EnsureNotArchived();
        if (Icon == icon) return;
        Icon = icon;
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

    public void UpdateStartDate(DateTimeOffset? startDate)
    {
        EnsureNotArchived();
        if (StartDate == startDate) return;
        StartDate = startDate;
        UpdateTimestamp();
    }
    public void UpdateDueDate(DateTimeOffset? dueDate)
    {
        EnsureNotArchived();
        if (DueDate == dueDate) return;
        DueDate = dueDate;
        UpdateTimestamp();
    }
    public void UpdateStoryPoints(int? storyPoints)
    {
        EnsureNotArchived();
        if (StoryPoints == storyPoints) return;
        StoryPoints = storyPoints;
        UpdateTimestamp();
    }
    public void UpdateTimeEstimate(long? timeEstimateSeconds)
    {
        EnsureNotArchived();
        if (TimeEstimateSeconds == timeEstimateSeconds) return;
        TimeEstimateSeconds = timeEstimateSeconds;
        UpdateTimestamp();
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
}
