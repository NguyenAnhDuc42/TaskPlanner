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
    private const int MAX_BATCH_SIZE = 200; // Safety limit for subtask batch ops

    public Guid ProjectWorkspaceId { get; private set; }
    public Guid ProjectSpaceId { get; private set; }
    public Guid? ProjectFolderId { get; private set; }
    public Guid ProjectListId { get; private set; }

    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public Guid CreatorId { get; private set; }

    public Guid? StatusId { get; private set; }
    public bool IsCompleted { get; private set; }
    public bool IsArchived { get; private set; }

    public Priority Priority { get; private set; }
    public DateTime? StartDate { get; private set; }
    public DateTime? DueDate { get; private set; }
    public int? StoryPoints { get; private set; }
    public long? TimeEstimateSeconds { get; private set; }
    public int? OrderIndex { get; private set; }
    public Visibility Visibility { get; private set; }

    public Guid? ParentTaskId { get; private set; }
    private readonly List<ProjectTask> _subtasks = new();
    public IReadOnlyCollection<ProjectTask> Subtasks => _subtasks.AsReadOnly();

    private readonly List<UserProjectTask> _assignees = new();
    public IReadOnlyCollection<UserProjectTask> Assignees => _assignees.AsReadOnly();

    private readonly List<ProjectTaskTag> _tags = new();
    public IReadOnlyCollection<ProjectTaskTag> Tags => _tags.AsReadOnly();

    private readonly List<Comment> _comments = new();
    public IReadOnlyCollection<Comment> Comments => _comments.AsReadOnly();

    private readonly List<Attachment> _attachments = new();
    public IReadOnlyCollection<Attachment> Attachments => _attachments.AsReadOnly();

    private readonly List<TimeLog> _timeLogs = new();
    public IReadOnlyCollection<TimeLog> TimeLogs => _timeLogs.AsReadOnly();

    private readonly List<Checklist> _checklists = new();
    public IReadOnlyCollection<Checklist> Checklists => _checklists.AsReadOnly();

    private ProjectTask() { } // EF Core

    internal ProjectTask(Guid id, Guid projectWorkspaceId, Guid projectSpaceId,
        Guid? projectFolderId, Guid projectListId, string name, string? description,
        Priority priority, DateTime? startDate, DateTime? dueDate, Visibility visibility,
        int orderIndex, Guid creatorId, Guid? parentTaskId = null)
    {
        Id = id;
        ProjectWorkspaceId = projectWorkspaceId;
        ProjectSpaceId = projectSpaceId;
        ProjectFolderId = projectFolderId;
        ProjectListId = projectListId;
        Name = name;
        Description = description;
        Priority = priority;
        StartDate = startDate;
        DueDate = dueDate;
        Visibility = visibility;
        OrderIndex = orderIndex;
        CreatorId = creatorId;
        ParentTaskId = parentTaskId;

        IsCompleted = false;
        IsArchived = false;
        StatusId = null;
        StoryPoints = null;
        TimeEstimateSeconds = null;
    }

    // === SELF MANAGEMENT ===

    public void UpdateBasicInfo(string name, string? description)
    {
        name = name?.Trim() ?? string.Empty;
        description = string.IsNullOrWhiteSpace(description?.Trim()) ? null : description.Trim();

        if (Name == name && Description == description) return;

        ValidateBasicInfo(name, description);

        var oldName = Name;
        var oldDescription = Description;
        Name = name;
        Description = description;
        UpdateTimestamp();
        AddDomainEvent(new TaskBasicInfoUpdatedEvent(Id, oldName, name, oldDescription, description));
    }

    public void UpdateSchedule(DateTime? startDate, DateTime? dueDate)
    {
        if (StartDate == startDate && DueDate == dueDate) return;

        ValidateSchedule(startDate, dueDate);

        var oldStartDate = StartDate;
        var oldDueDate = DueDate;
        StartDate = startDate;
        DueDate = dueDate;
        UpdateTimestamp();
        AddDomainEvent(new TaskScheduleUpdatedEvent(Id, oldStartDate, startDate, oldDueDate, dueDate));
    }

    public void ChangePriority(Priority newPriority)
    {
        if (Priority == newPriority) return;

        var oldPriority = Priority;
        Priority = newPriority;
        UpdateTimestamp();
        AddDomainEvent(new TaskPriorityChangedEvent(Id, oldPriority, newPriority));
    }

    public void ChangeVisibility(Visibility newVisibility)
    {
        if (Visibility == newVisibility) return;

        var oldVisibility = Visibility;
        Visibility = newVisibility;
        UpdateTimestamp();
        AddDomainEvent(new TaskVisibilityChangedEvent(Id, oldVisibility, newVisibility));

        CascadeVisibilityToSubtasks(newVisibility);
    }

    public void UpdateStatus(Guid statusId, bool isCompletedStatus)
    {
        if (StatusId == statusId && IsCompleted == isCompletedStatus) return;

        var oldStatusId = StatusId;
        var wasCompleted = IsCompleted;

        StatusId = statusId;
        IsCompleted = isCompletedStatus;

        UpdateTimestamp();
        AddDomainEvent(new TaskStatusUpdatedEvent(Id, oldStatusId, statusId, isCompletedStatus));

        if (!wasCompleted && isCompletedStatus)
            AddDomainEvent(new TaskCompletedEvent(Id, DateTime.UtcNow));
        else if (wasCompleted && !isCompletedStatus)
            AddDomainEvent(new TaskReopenedEvent(Id, DateTime.UtcNow));
    }

    public void SetEstimation(int? storyPoints, long? timeEstimateSeconds)
    {
        ValidateEstimation(storyPoints, timeEstimateSeconds);

        if (StoryPoints == storyPoints && TimeEstimateSeconds == timeEstimateSeconds) return;

        var oldStoryPoints = StoryPoints;
        var oldTimeEstimate = TimeEstimateSeconds;
        StoryPoints = storyPoints;
        TimeEstimateSeconds = timeEstimateSeconds;
        UpdateTimestamp();
        AddDomainEvent(new TaskEstimationUpdatedEvent(Id, oldStoryPoints, storyPoints, oldTimeEstimate, timeEstimateSeconds));
    }

    internal void UpdateOrderIndex(int newOrderIndex)
    {
        if (OrderIndex == newOrderIndex) return;

        OrderIndex = newOrderIndex;
        UpdateTimestamp();
    }

    public void Archive()
    {
        if (IsArchived) return;

        IsArchived = true;
        UpdateTimestamp();
        AddDomainEvent(new TaskArchivedEvent(Id));

        ArchiveAllSubtasks();
    }

    public void Unarchive()
    {
        if (!IsArchived) return;

        IsArchived = false;
        UpdateTimestamp();
        AddDomainEvent(new TaskUnarchivedEvent(Id));

        UnarchiveAllSubtasks();
    }

    // === SUBTASK MANAGEMENT ===

    public ProjectTask CreateSubtask(string name, string? description, Priority priority)
    {
        if (IsArchived)
            throw new InvalidOperationException("Cannot create subtasks for archived tasks.");

        name = name?.Trim() ?? string.Empty;
        description = string.IsNullOrWhiteSpace(description?.Trim()) ? null : description.Trim();

        ValidateBasicInfo(name, description);

        var orderIndex = _subtasks.Count;
        var subtask = new ProjectTask(Guid.NewGuid(), ProjectWorkspaceId, ProjectSpaceId,
            ProjectFolderId, ProjectListId, name, description, priority,
            null, null, Visibility, orderIndex, CreatorId, Id);

        _subtasks.Add(subtask);
        UpdateTimestamp();
        AddDomainEvent(new SubtaskCreatedEvent(Id, subtask.Id, name, CreatorId));
        return subtask;
    }

    public void RemoveSubtask(Guid subtaskId)
    {
        var subtask = _subtasks.FirstOrDefault(s => s.Id == subtaskId);
        if (subtask == null)
            throw new InvalidOperationException("Subtask not found.");

        _subtasks.Remove(subtask);
        UpdateTimestamp();
        AddDomainEvent(new SubtaskRemovedEvent(Id, subtaskId, subtask.Name));
    }

    // === SUBTASK BULK ===

    public void ArchiveAllSubtasks()
    {
        var subtasksToArchive = _subtasks.Where(s => !s.IsArchived).ToList();
        if (!subtasksToArchive.Any()) return;

        if (subtasksToArchive.Count > MAX_BATCH_SIZE)
            throw new InvalidOperationException($"Batch too large. Reduce size below {MAX_BATCH_SIZE} or run as smaller batches.");

        foreach (var subtask in subtasksToArchive)
        {
            subtask.Archive();
        }

        UpdateTimestamp();
        AddDomainEvent(new AllSubtasksArchivedEvent(Id));
    }

    public void UnarchiveAllSubtasks()
    {
        var subtasksToUnarchive = _subtasks.Where(s => s.IsArchived).ToList();
        if (!subtasksToUnarchive.Any()) return;

        if (subtasksToUnarchive.Count > MAX_BATCH_SIZE)
            throw new InvalidOperationException($"Batch too large. Reduce size below {MAX_BATCH_SIZE} or run as smaller batches.");

        foreach (var subtask in subtasksToUnarchive)
        {
            subtask.Unarchive();
        }

        UpdateTimestamp();
        AddDomainEvent(new AllSubtasksUnarchivedEvent(Id));
    }

    public void CompleteAllSubtasks()
    {
        // TODO: MOVE_TO_HANDLER: completing subtasks requires workspace default status resolution.
        throw new InvalidOperationException("This method was moved to application handler. See TODO: MOVE_TO_HANDLER: CompleteAllSubtasks");
    }

    public void AssignAllSubtasksToUser(Guid userId)
    {
        ValidateGuid(userId, nameof(userId));

        var subtasksToAssign = _subtasks.Where(s => !s.IsAssignedTo(userId) && !s.IsArchived).ToList();
        if (!subtasksToAssign.Any()) return;

        if (subtasksToAssign.Count > MAX_BATCH_SIZE)
            throw new InvalidOperationException($"Batch too large. Reduce size below {MAX_BATCH_SIZE} or run as smaller batches.");

        foreach (var subtask in subtasksToAssign)
        {
            subtask.AssignUser(userId);
        }

        UpdateTimestamp();
        AddDomainEvent(new AllSubtasksAssignedToUserEvent(Id, userId));
    }

    public void UpdateAllSubtasksPriority(Priority newPriority)
    {
        var subtasksToUpdate = _subtasks.Where(s => s.Priority != newPriority && !s.IsArchived).ToList();
        if (!subtasksToUpdate.Any()) return;

        if (subtasksToUpdate.Count > MAX_BATCH_SIZE)
            throw new InvalidOperationException($"Batch too large. Reduce size below {MAX_BATCH_SIZE} or run as smaller batches.");

        foreach (var subtask in subtasksToUpdate)
        {
            subtask.ChangePriority(newPriority);
        }

        UpdateTimestamp();
        AddDomainEvent(new AllSubtasksPriorityUpdatedEvent(Id, newPriority));
    }

    // === ASSIGNMENT ===

    public void AssignUser(Guid userId)
    {
        ValidateGuid(userId, nameof(userId));

        if (_assignees.Any(a => a.UserId == userId))
            throw new InvalidOperationException("User is already assigned to this task.");

        var assignment = UserProjectTask.Create(userId, Id);
        _assignees.Add(assignment);
        UpdateTimestamp();
        AddDomainEvent(new UserAssignedToTaskEvent(Id, userId));
    }

    public void UnassignUser(Guid userId)
    {
        var assignment = _assignees.FirstOrDefault(a => a.UserId == userId);
        if (assignment == null) return;

        _assignees.Remove(assignment);
        UpdateTimestamp();
        AddDomainEvent(new UserUnassignedFromTaskEvent(Id, userId));
    }

    public bool IsAssignedTo(Guid userId)
    {
        ValidateGuid(userId, nameof(userId));
        return _assignees.Any(a => a.UserId == userId);
    }

    // === SUPPORT ENTITIES ===

    public Comment AddComment(string content, Guid authorId)
    {
        content = content?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Comment content cannot be empty.", nameof(content));
        ValidateGuid(authorId, nameof(authorId));

        var comment = Comment.Create(content, authorId, Id);
        _comments.Add(comment);
        UpdateTimestamp();
        AddDomainEvent(new CommentAddedToTaskEvent(Id, comment.Id, authorId, content));
        return comment;
    }

    public Attachment AddAttachment(string fileName, string fileUrl, string? fileType, long fileSize, Guid uploaderId)
    {
        fileName = fileName?.Trim() ?? string.Empty;
        fileUrl = fileUrl?.Trim() ?? string.Empty;
        fileType = string.IsNullOrWhiteSpace(fileType?.Trim()) ? null : fileType.Trim();

        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name cannot be empty.", nameof(fileName));
        if (string.IsNullOrWhiteSpace(fileUrl))
            throw new ArgumentException("File URL cannot be empty.", nameof(fileUrl));
        ValidateGuid(uploaderId, nameof(uploaderId));

        var attachment = Attachment.Create(fileName, fileUrl, fileType, uploaderId, Id);
        _attachments.Add(attachment);
        UpdateTimestamp();
        AddDomainEvent(new AttachmentAddedToTaskEvent(Id, attachment.Id, fileName, fileUrl));
        return attachment;
    }

    public TimeLog LogTime(TimeSpan duration, Guid userId, string? description = null)
    {
        description = string.IsNullOrWhiteSpace(description?.Trim()) ? null : description.Trim();

        if (duration <= TimeSpan.Zero)
            throw new ArgumentException("Duration must be positive.", nameof(duration));
        ValidateGuid(userId, nameof(userId));

        var timeLog = TimeLog.Create(duration, userId, Id);
        _timeLogs.Add(timeLog);
        UpdateTimestamp();
        AddDomainEvent(new TimeLogAddedToTaskEvent(Id, timeLog.Id, duration, userId));
        return timeLog;
    }

    public Checklist CreateChecklist(string title)
    {
        title = title?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Checklist title cannot be empty.", nameof(title));

        var checklist = Checklist.Create(title, Id);
        _checklists.Add(checklist);
        UpdateTimestamp();
        AddDomainEvent(new ChecklistAddedToTaskEvent(Id, checklist.Id, title));
        return checklist;
    }

    // === TAGS ===

    public void AddTag(Guid tagId, Guid workspaceId)
    {
        ValidateGuid(tagId, nameof(tagId));
        ValidateGuid(workspaceId, nameof(workspaceId));

        if (ProjectWorkspaceId != workspaceId)
            throw new InvalidOperationException("Cannot add a tag from another workspace.");

        if (_tags.Any(t => t.TagId == tagId)) return;

        var tag = ProjectTaskTag.Create(Id, tagId);
        _tags.Add(tag);
        UpdateTimestamp();
        AddDomainEvent(new TagAddedToTaskEvent(Id, tagId));
    }

    public void RemoveTag(Guid tagId)
    {
        var tag = _tags.FirstOrDefault(t => t.TagId == tagId);
        if (tag == null) return;

        _tags.Remove(tag);
        UpdateTimestamp();
        AddDomainEvent(new TagRemovedFromTaskEvent(Id, tagId));
    }
    // === MOVEMENT ===

    internal void MoveToList(Guid newListId, Guid? newFolderId = null, Guid? newSpaceId = null)
    {
        newFolderId ??= ProjectFolderId;
        newSpaceId ??= ProjectSpaceId;

        if (ProjectListId == newListId && ProjectFolderId == newFolderId && ProjectSpaceId == newSpaceId)
            return;

        var oldListId = ProjectListId;
        var oldFolderId = ProjectFolderId;
        var oldSpaceId = ProjectSpaceId;

        ProjectListId = newListId;
        ProjectFolderId = newFolderId;
        ProjectSpaceId = newSpaceId.Value;

        UpdateTimestamp();
        AddDomainEvent(new TaskMovedEvent(Id, oldListId, newListId, oldFolderId, newFolderId, oldSpaceId, newSpaceId));
    }

    // === PRIVATE CASCADE HELPERS ===

    private void CascadeVisibilityToSubtasks(Visibility newVisibility)
    {
        if (newVisibility == Visibility.Private)
        {
            foreach (var s in _subtasks.Where(s => s.Visibility == Visibility.Public))
                s.ChangeVisibility(Visibility.Private);
        }
    }

    // === VALIDATION HELPERS ===

    private static void ValidateBasicInfo(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Task name cannot be empty.", nameof(name));
        if (name.Length > 200)
            throw new ArgumentException("Task name cannot exceed 200 characters.", nameof(name));
        if (description?.Length > 2000)
            throw new ArgumentException("Task description cannot exceed 2000 characters.", nameof(description));
    }

    private static void ValidateSchedule(DateTime? startDate, DateTime? dueDate)
    {
        if (startDate.HasValue && dueDate.HasValue && startDate > dueDate)
            throw new ArgumentException("Start date cannot be later than due date.", nameof(startDate));
    }

    private static void ValidateEstimation(int? storyPoints, long? timeEstimateSeconds)
    {
        if (storyPoints.HasValue && storyPoints < 0) throw new ArgumentOutOfRangeException(nameof(storyPoints));
        if (timeEstimateSeconds.HasValue && timeEstimateSeconds < 0) throw new ArgumentOutOfRangeException(nameof(timeEstimateSeconds));
    }

    private static void ValidateGuid(Guid id, string paramName)
    {
        if (id == Guid.Empty) throw new ArgumentException("Guid cannot be empty.", paramName);
    }
}
