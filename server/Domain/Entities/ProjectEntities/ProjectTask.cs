using Domain.Common;
using Domain.Entities.Relationship;
using Domain.Entities.Support;
using Domain.Enums;
using Domain.Events.TaskEvents;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Domain.Entities.ProjectEntities;

public class ProjectTask : Aggregate
{
    // Hierarchy for quick queries - "show all tasks in workspace X"
    public Guid ProjectWorkspaceId { get; private set; }
    public Guid ProjectSpaceId { get; private set; }
    public Guid? ProjectFolderId { get; private set; }
    public Guid ProjectListId { get; private set; } // Direct parent

    // Task identity
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public Guid CreatorId { get; private set; } // Who created this task

    // Workflow state
    public Guid? StatusId { get; private set; }
    public bool IsCompleted { get; private set; }
    public bool IsArchived { get; private set; }

    // Task properties
    public Priority Priority { get; private set; }
    public DateTime? StartDate { get; private set; }
    public DateTime? DueDate { get; private set; }
    public int? StoryPoints { get; private set; }
    public TimeSpan? TimeEstimate { get; private set; }
    public int OrderIndex { get; private set; }
    public Visibility Visibility { get; private set; }

    // Task hierarchy
    public Guid? ParentTaskId { get; private set; }
    private readonly List<ProjectTask> _subtasks = new();
    public IReadOnlyCollection<ProjectTask> Subtasks => _subtasks.AsReadOnly();

    // Assignments & watchers
    private readonly List<UserProjectTask> _assignees = new();
    public IReadOnlyCollection<UserProjectTask> Assignees => _assignees.AsReadOnly();

    private readonly List<ProjectTaskWatcher> _watchers = new();
    public IReadOnlyCollection<ProjectTaskWatcher> Watchers => _watchers.AsReadOnly();

    // Tags
    private readonly List<ProjectTaskTag> _tags = new();
    public IReadOnlyCollection<ProjectTaskTag> Tags => _tags.AsReadOnly();

    // Support entities
    private readonly List<Comment> _comments = new();
    public IReadOnlyCollection<Comment> Comments => _comments.AsReadOnly();

    private readonly List<Attachment> _attachments = new();
    public IReadOnlyCollection<Attachment> Attachments => _attachments.AsReadOnly();

    private readonly List<TimeLog> _timeLogs = new();
    public IReadOnlyCollection<TimeLog> TimeLogs => _timeLogs.AsReadOnly();

    private readonly List<Checklist> _checklists = new();
    public IReadOnlyCollection<Checklist> Checklists => _checklists.AsReadOnly();

    // Constructors
    private ProjectTask() { } // EF Core

    private ProjectTask(Guid id, string name, string? description, Priority priority,
        DateTime? startDate, DateTime? dueDate, Visibility visibility,
        Guid projectWorkspaceId, Guid projectSpaceId, Guid? projectFolderId,
        Guid projectListId, Guid creatorId)
    {
        Id = id;
        Name = name;
        Description = description;
        Priority = priority;
        StartDate = startDate;
        DueDate = dueDate;
        Visibility = visibility;
        ProjectWorkspaceId = projectWorkspaceId;
        ProjectSpaceId = projectSpaceId;
        ProjectFolderId = projectFolderId;
        ProjectListId = projectListId;
        CreatorId = creatorId;

        OrderIndex = 0;
        IsCompleted = false;
        StatusId = null;
        StoryPoints = null;
        TimeEstimate = null;
    }

    // Factory
    public static ProjectTask Create(string name, string? description, Priority priority,
        DateTime? startDate, DateTime? dueDate, Visibility visibility,
        Guid projectWorkspaceId, Guid projectSpaceId, Guid? projectFolderId,
        Guid projectListId, Guid creatorId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Task name cannot be empty.", nameof(name));

        var task = new ProjectTask(Guid.NewGuid(), name.Trim(), description?.Trim(), priority,
            startDate, dueDate, visibility, projectWorkspaceId, projectSpaceId,
            projectFolderId, projectListId, creatorId);

        task.AddDomainEvent(new TaskCreatedEvent(task.Id, task.Name, task.CreatorId, task.ProjectListId));
        return task;
    }

    // Business logic
    public void UpdateName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Task name cannot be empty.", nameof(newName));
        if (Name == newName) return;

        Name = newName.Trim();
        AddDomainEvent(new TaskNameUpdatedEvent(Id, Name));
    }

    public void UpdateDescription(string? newDescription)
    {
        if (Description == newDescription) return;

        if (newDescription != null && string.IsNullOrWhiteSpace(newDescription))
            throw new ArgumentException("Task description cannot be whitespace.", nameof(newDescription));

        Description = newDescription?.Trim();
        AddDomainEvent(new TaskDescriptionUpdatedEvent(Id, Description));
    }

    public void ChangePriority(Priority newPriority)
    {
        if (Priority == newPriority) return;
        Priority = newPriority;
        AddDomainEvent(new TaskPriorityChangedEvent(Id, newPriority));
    }

    public void SetDueDate(DateTime? newDueDate)
    {
        if (DueDate == newDueDate) return;
        DueDate = newDueDate;
        AddDomainEvent(new TaskDueDateSetEvent(Id, newDueDate));
    }

    public void UpdateStatus(Guid newStatusId, bool isCompleted = false)
    {
        if (StatusId == newStatusId && IsCompleted == isCompleted) return;

        StatusId = newStatusId;
        IsCompleted = isCompleted;
        AddDomainEvent(new TaskStatusUpdatedEvent(Id, newStatusId));
    }

    public void CompleteTask(Guid doneStatusId)
    {
        if (IsCompleted) return;

        StatusId = doneStatusId;
        IsCompleted = true;
        AddDomainEvent(new TaskCompletedEvent(Id));
    }

    // Comments / Attachments / Checklists / TimeLogs
    public Comment AddComment(string content, Guid authorId)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Comment content cannot be empty.", nameof(content));

        var comment = Comment.Create(content.Trim(), authorId, Id);
        _comments.Add(comment);
        AddDomainEvent(new CommentAddedToTaskEvent(Id, comment.Id, authorId, comment.Content));
        return comment;
    }

    public Attachment AddAttachment(string fileName, string fileUrl, string fileType, Guid uploaderId)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("Attachment file name cannot be empty.", nameof(fileName));
        if (string.IsNullOrWhiteSpace(fileUrl))
            throw new ArgumentException("Attachment file URL cannot be empty.", nameof(fileUrl));

        var attachment = Attachment.Create(fileName.Trim(), fileUrl.Trim(), fileType.Trim(), uploaderId, Id);
        _attachments.Add(attachment);
        AddDomainEvent(new AttachmentAddedToTaskEvent(Id, attachment.Id, fileName, fileUrl));
        return attachment;
    }

    public Checklist AddChecklist(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Checklist title cannot be empty.", nameof(title));

        var checklist = Checklist.Create(title.Trim(), Id);
        _checklists.Add(checklist);
        AddDomainEvent(new ChecklistAddedToTaskEvent(Id, checklist.Id, checklist.Title));
        return checklist;
    }

    public TimeLog AddTimeLog(TimeSpan timeSpent, Guid userId)
    {
        if (timeSpent <= TimeSpan.Zero)
            throw new ArgumentException("Time spent must be greater than zero.", nameof(timeSpent));

        var timeLog = TimeLog.Create(timeSpent, userId, Id);
        _timeLogs.Add(timeLog);
        AddDomainEvent(new TimeLogAddedToTaskEvent(Id, timeLog.Id, timeSpent, userId));
        return timeLog;
    }

    // Assignment & watching
    public void AssignUser(Guid userId)
    {
        if (_assignees.Any(a => a.UserId == userId)) return;
        _assignees.Add(UserProjectTask.Create(userId, Id));
        AddDomainEvent(new UserAssignedToTaskEvent(Id, userId));
    }

    public void RemoveAssignee(Guid userId)
    {
        var assignee = _assignees.FirstOrDefault(a => a.UserId == userId);
        if (assignee == null) return;

        _assignees.Remove(assignee);
        AddDomainEvent(new UserUnassignedFromTaskEvent(Id, userId));
    }

    public void AddWatcher(Guid userId)
    {
        if (_watchers.Any(w => w.UserId == userId)) return;
        _watchers.Add(ProjectTaskWatcher.Create(Id, userId));
        AddDomainEvent(new WatcherAddedToTaskEvent(Id, userId));
    }

    public void RemoveWatcher(Guid userId)
    {
        var watcher = _watchers.FirstOrDefault(w => w.UserId == userId);
        if (watcher == null) return;

        _watchers.Remove(watcher);
        AddDomainEvent(new WatcherRemovedFromTaskEvent(Id, userId));
    }

    // Tags
    public void AddTag(Guid tagId)
    {
        if (_tags.Any(t => t.TagId == tagId)) return;

        var tag = new ProjectTaskTag(Id, tagId);
        _tags.Add(tag);
        AddDomainEvent(new TagAddedToTaskEvent(Id, tagId, string.Empty));
    }

    public void RemoveTag(Guid tagId)
    {
        var tag = _tags.FirstOrDefault(t => t.TagId == tagId);
        if (tag == null) return;

        _tags.Remove(tag);
        AddDomainEvent(new TagRemovedFromTaskEvent(Id, tagId));
    }

    // Archive / Move
    public void Archive()
    {
        if (IsArchived) return;
        IsArchived = true;
        AddDomainEvent(new TaskArchivedEvent(Id));
    }

    public void Unarchive()
    {
        if (!IsArchived) return;
        IsArchived = false;
        AddDomainEvent(new TaskUnarchivedEvent(Id));
    }

    public void MoveTo(Guid newListId, Guid? newFolderId, Guid newSpaceId)
    {
        if (ProjectListId == newListId && ProjectFolderId == newFolderId && ProjectSpaceId == newSpaceId) return;

        var oldListId = ProjectListId;
        ProjectListId = newListId;
        ProjectFolderId = newFolderId;
        ProjectSpaceId = newSpaceId;

        AddDomainEvent(new TaskMovedEvent(Id, oldListId, newListId));
    }

    // Estimation
    public void SetStoryPoints(int? points)
    {
        if (points.HasValue && points < 0)
            throw new ArgumentOutOfRangeException(nameof(points), "Story points cannot be negative.");
        if (StoryPoints == points) return;

        StoryPoints = points;
        AddDomainEvent(new TaskStoryPointsUpdatedEvent(Id, points));
    }

    public void SetTimeEstimate(TimeSpan? estimate)
    {
        if (estimate.HasValue && estimate.Value < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(estimate), "Time estimate cannot be negative.");
        if (TimeEstimate == estimate) return;

        TimeEstimate = estimate;
        AddDomainEvent(new TaskTimeEstimateUpdatedEvent(Id, estimate?.Ticks));
    }
}
