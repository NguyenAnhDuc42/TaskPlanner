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
    
    // Workflow state - links to workspace's defined statuses
    public Guid? StatusId { get; private set; }
    public bool IsCompleted { get; private set; } // Derived from status
    
    // Task properties
    public Priority Priority { get; private set; }
    public DateTime? StartDate { get; private set; }
    public DateTime? DueDate { get; private set; }
    public int? StoryPoints { get; private set; }
    public TimeSpan? TimeEstimate { get; private set; }
    public int OrderIndex { get; private set; }
    public Visibility Visibility { get; private set; }
    
    // Task hierarchy (subtasks)
    public Guid? ParentTaskId { get; private set; }
    private readonly List<ProjectTask> _subtasks = new();
    public IReadOnlyCollection<ProjectTask> Subtasks => _subtasks.AsReadOnly();
    
    // Task assignments and watchers
    private readonly List<UserProjectTask> _assignees = new();
    public IReadOnlyCollection<UserProjectTask> Assignees => _assignees.AsReadOnly();
    
    private readonly List<ProjectTaskWatcher> _watchers = new();
    public IReadOnlyCollection<ProjectTaskWatcher> Watchers => _watchers.AsReadOnly();
    
    // Task tags
    private readonly List<ProjectTaskTag> _tags = new();
    public IReadOnlyCollection<ProjectTaskTag> Tags => _tags.AsReadOnly();
    
    // Support entities that belong TO this task
    private readonly List<Comment> _comments = new();
    public IReadOnlyCollection<Comment> Comments => _comments.AsReadOnly();
    
    private readonly List<Attachment> _attachments = new();
    public IReadOnlyCollection<Attachment> Attachments => _attachments.AsReadOnly();
    
    private readonly List<TimeLog> _timeLogs = new();
    public IReadOnlyCollection<TimeLog> TimeLogs => _timeLogs.AsReadOnly();
    
    private readonly List<Checklist> _checklists = new();
    public IReadOnlyCollection<Checklist> Checklists => _checklists.AsReadOnly();

    // Constructors
    private ProjectTask() { } // For EF Core

    private ProjectTask(Guid id, string name, string? description, Priority priority, DateTime? startDate, DateTime? dueDate, Visibility visibility, Guid projectWorkspaceId, Guid projectSpaceId, Guid? projectFolderId, Guid projectListId, Guid creatorId)
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

        // Initialize other declared properties with default values
        OrderIndex = 0;
        IsCompleted = false;
        StatusId = null;
        StoryPoints = null;
        TimeEstimate = null;
    }

    // Static Factory Method
    public static ProjectTask Create(string name, string? description, Priority priority, DateTime? startDate, DateTime? dueDate, Visibility visibility, Guid projectWorkspaceId, Guid projectSpaceId, Guid? projectFolderId, Guid projectListId, Guid creatorId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Task name cannot be empty.", nameof(name));

        var task = new ProjectTask(Guid.NewGuid(), name, description, priority, startDate, dueDate, visibility, projectWorkspaceId, projectSpaceId, projectFolderId, projectListId, creatorId);
        task.AddDomainEvent(new TaskCreatedEvent(task.Id, task.Name, task.CreatorId, task.ProjectListId));
        return task;
    }

    // Business Methods
    public void UpdateName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Task name cannot be empty.", nameof(newName));
        if (Name == newName) return;

        Name = newName;
        AddDomainEvent(new TaskNameUpdatedEvent(Id, newName));
    }

    public void UpdateDescription(string newDescription)
    {
        if (Description == newDescription) return;

        Description = newDescription;
        AddDomainEvent(new TaskDescriptionUpdatedEvent(Id, newDescription));
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

    public void UpdateStatus(Guid newStatusId)
    {
        if (StatusId == newStatusId) return;

        StatusId = newStatusId;
        AddDomainEvent(new TaskStatusUpdatedEvent(Id, newStatusId));
    }

    public void CompleteTask()
    {
        // Assuming a 'Done' status exists and can be set
        // This would typically involve finding the 'Done' status ID
        // For now, we'll just raise the event
        AddDomainEvent(new TaskCompletedEvent(Id));
    }

    public Comment AddComment(string content, Guid authorId)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Comment content cannot be empty.", nameof(content));

        var comment = Comment.Create(content, authorId, Id);
        _comments.Add(comment);
        AddDomainEvent(new CommentAddedToTaskEvent(Id, comment.Id, authorId, content));
        return comment;
    }

    public Attachment AddAttachment(string fileName, string fileUrl, string fileType, Guid uploaderId)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("Attachment file name cannot be empty.", nameof(fileName));
        if (string.IsNullOrWhiteSpace(fileUrl))
            throw new ArgumentException("Attachment file URL cannot be empty.", nameof(fileUrl));

        var attachment = Attachment.Create(fileName, fileUrl, fileType, uploaderId, Id);
        _attachments.Add(attachment);
        AddDomainEvent(new AttachmentAddedToTaskEvent(Id, attachment.Id, fileName, fileUrl));
        return attachment;
    }

    public Checklist AddChecklist(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Checklist title cannot be empty.", nameof(title));

        var checklist = Checklist.Create(title, Id);
        _checklists.Add(checklist);
        AddDomainEvent(new ChecklistAddedToTaskEvent(Id, checklist.Id, title));
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

    public void AssignUser(Guid userId)
    {
        if (_assignees.Any(a => a.UserId == userId)) return; // Already assigned
        _assignees.Add(UserProjectTask.Create(userId, Id));
        AddDomainEvent(new UserAssignedToTaskEvent(Id, userId));
    }

    public void RemoveAssignee(Guid userId)
    {
        var assignee = _assignees.FirstOrDefault(a => a.UserId == userId);
        if (assignee is null) return;
        _assignees.Remove(assignee);
        AddDomainEvent(new UserUnassignedFromTaskEvent(Id, userId));
    }

    public void AddWatcher(Guid userId)
    {
        if (_watchers.Any(w => w.UserId == userId)) return; // Already watching
        _watchers.Add(ProjectTaskWatcher.Create(Id, userId));
        AddDomainEvent(new WatcherAddedToTaskEvent(Id, userId));
    }

    public void RemoveWatcher(Guid userId)
    {
        var watcher = _watchers.FirstOrDefault(w => w.UserId == userId);
        if (watcher is null) return;
        _watchers.Remove(watcher);
        AddDomainEvent(new WatcherRemovedFromTaskEvent(Id, userId));
    }

    public void AddTag(Guid tagId)
    {
        if (_projectTaskTags.Any(ptt => ptt.TagId == tagId)) return; // Already tagged

        _projectTaskTags.Add(new ProjectTaskTag(Id, tagId));
        AddDomainEvent(new TagAddedToTaskEvent(Id, tagId, "")); // Name is not available here, might need adjustment
    }

    public void RemoveTag(Guid tagId)
    {
        var projectTaskTag = _projectTaskTags.FirstOrDefault(ptt => ptt.TagId == tagId);
        if (projectTaskTag is null) return;
        _projectTaskTags.Remove(projectTaskTag);
        AddDomainEvent(new TagRemovedFromTaskEvent(Id, tagId));
    }

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
        if (ProjectListId == newListId) return;

        var oldListId = ProjectListId;
        ProjectListId = newListId;
        ProjectFolderId = newFolderId;
        ProjectSpaceId = newSpaceId;
        AddDomainEvent(new TaskMovedEvent(Id, oldListId, newListId));
    }

    public void SetStoryPoints(int? points)
    {
        if (points.HasValue && points < 0)
            throw new ArgumentOutOfRangeException(nameof(points), "Story points cannot be negative.");
        if (StoryPoints == points) return;

        StoryPoints = points;
        AddDomainEvent(new TaskStoryPointsUpdatedEvent(Id, points));
    }

    public void SetTimeEstimate(long? estimateInSeconds)
    {
        if (estimateInSeconds.HasValue && estimateInSeconds < 0)
            throw new ArgumentOutOfRangeException(nameof(estimateInSeconds), "Time estimate cannot be negative.");
        if (TimeEstimate == estimateInSeconds) return;

        TimeEstimate = estimateInSeconds;
        AddDomainEvent(new TaskTimeEstimateUpdatedEvent(Id, estimateInSeconds));
    }
}