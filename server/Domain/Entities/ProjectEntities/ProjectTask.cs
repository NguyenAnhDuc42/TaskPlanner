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
    // Hierarchy for quick queries
    public Guid ProjectWorkspaceId { get; private set; }
    public Guid ProjectSpaceId { get; private set; }
    public Guid? ProjectFolderId { get; private set; }
    public Guid ProjectListId { get; private set; }

    // Task identity
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public Guid CreatorId { get; private set; }

    // Workflow state
    public Guid? StatusId { get; private set; }
    public bool IsCompleted { get; private set; }
    public bool IsArchived { get; private set; }

    // Task properties
    public Priority Priority { get; private set; }
    public DateTime? StartDate { get; private set; }
    public DateTime? DueDate { get; private set; }
    public int? StoryPoints { get; private set; }
    public long? TimeEstimateSeconds { get; private set; } // Simple seconds
    public int? OrderIndex { get; private set; } // Nullable for proper ordering
    public Visibility Visibility { get; private set; }

    // Task hierarchy - subtasks
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

    // Support entities - these belong to the task
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

    // Internal constructor - only called by parent list
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
        
        // Defaults
        IsCompleted = false;
        IsArchived = false;
        StatusId = null; // Will be set to workspace default
        StoryPoints = null;
        TimeEstimateSeconds = null;
    }

    // === SELF MANAGEMENT METHODS ===

    public void UpdateBasicInfo(string name, string? description)
    {
        // Normalize inputs first
        name = name?.Trim() ?? string.Empty;
        description = string.IsNullOrWhiteSpace(description?.Trim()) ? null : description.Trim();
        
        // Check for changes first
        if (Name == name && Description == description) return;
        
        // Then validate
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
        // Check for changes first
        if (StartDate == startDate && DueDate == dueDate) return;
        
        // Then validate
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
        // Check for changes first
        if (Priority == newPriority) return;
        
        var oldPriority = Priority;
        Priority = newPriority;
        UpdateTimestamp();
        AddDomainEvent(new TaskPriorityChangedEvent(Id, oldPriority, newPriority));
    }

    public void ChangeVisibility(Visibility newVisibility)
    {
        // Check for changes first
        if (Visibility == newVisibility) return;
        
        var oldVisibility = Visibility;
        Visibility = newVisibility;
        UpdateTimestamp();
        AddDomainEvent(new TaskVisibilityChangedEvent(Id, oldVisibility, newVisibility));
        
        // CASCADE: Update all subtasks to match task visibility if they were public
        CascadeVisibilityToSubtasks(newVisibility);
    }

    public void UpdateStatus(Guid statusId, bool isCompletedStatus)
    {
        // Check for changes first
        if (StatusId == statusId) return;

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
        // Validate first
        ValidateEstimation(storyPoints, timeEstimateSeconds);
        
        // Check for changes
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
        
        // CASCADE: Archive all subtasks
        ArchiveAllSubtasks();
    }

    public void Unarchive()
    {
        if (!IsArchived) return;
        
        IsArchived = false;
        UpdateTimestamp();
        AddDomainEvent(new TaskUnarchivedEvent(Id));
        
        // CASCADE: Unarchive all subtasks
        UnarchiveAllSubtasks();
    }

    // === SUBTASK MANAGEMENT ===

    public ProjectTask CreateSubtask(string name, string? description, Priority priority)
    {
        if (IsArchived)
            throw new InvalidOperationException("Cannot create subtasks for archived tasks.");
        
        // Normalize inputs
        name = name?.Trim() ?? string.Empty;
        description = string.IsNullOrWhiteSpace(description?.Trim()) ? null : description.Trim();
        
        // Validate
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

    // === BULK OPERATIONS FOR SUBTASKS (MISSING HIERARCHICAL METHODS) ===
    
    public void ArchiveAllSubtasks()
    {
        var subtasksToArchive = _subtasks.Where(s => !s.IsArchived).ToList();
        if (!subtasksToArchive.Any()) return;
        
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
        
        foreach (var subtask in subtasksToUnarchive)
        {
            subtask.Unarchive();
        }
        
        UpdateTimestamp();
        AddDomainEvent(new AllSubtasksUnarchivedEvent(Id));
    }
    
    public void CompleteAllSubtasks()
    {
        var subtasksToComplete = _subtasks.Where(s => !s.IsCompleted && !s.IsArchived).ToList();
        if (!subtasksToComplete.Any()) return;
        
        foreach (var subtask in subtasksToComplete)
        {
            // This would need a default "completed" status from the workspace
            // subtask.UpdateStatus(completedStatusId, true);
        }
        
        UpdateTimestamp();
        AddDomainEvent(new AllSubtasksCompletedEvent(Id));
    }
    
    public void AssignAllSubtasksToUser(Guid userId)
    {
        ValidateGuid(userId, nameof(userId));
        
        var subtasksToAssign = _subtasks.Where(s => !s.IsAssignedTo(userId) && !s.IsArchived).ToList();
        if (!subtasksToAssign.Any()) return;
        
        foreach (var subtask in subtasksToAssign)
        {
            try
            {
                subtask.AssignUser(userId);
            }
            catch (InvalidOperationException)
            {
                // Subtask might already be assigned - ignore
            }
        }
        
        UpdateTimestamp();
        AddDomainEvent(new AllSubtasksAssignedToUserEvent(Id, userId));
    }
    
    public void UpdateAllSubtasksPriority(Priority newPriority)
    {
        var subtasksToUpdate = _subtasks.Where(s => s.Priority != newPriority && !s.IsArchived).ToList();
        if (!subtasksToUpdate.Any()) return;
        
        foreach (var subtask in subtasksToUpdate)
        {
            subtask.ChangePriority(newPriority);
        }
        
        UpdateTimestamp();
        AddDomainEvent(new AllSubtasksPriorityUpdatedEvent(Id, newPriority));
    }

    // === ASSIGNMENT & WATCHING ===

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

    public void AddWatcher(Guid userId)
    {
        ValidateGuid(userId, nameof(userId));
            
        if (_watchers.Any(w => w.UserId == userId)) return; // Already watching

        var watcher = ProjectTaskWatcher.Create(Id, userId);
        _watchers.Add(watcher);
        UpdateTimestamp();
        AddDomainEvent(new WatcherAddedToTaskEvent(Id, userId));
    }

    public void RemoveWatcher(Guid userId)
    {
        var watcher = _watchers.FirstOrDefault(w => w.UserId == userId);
        if (watcher == null) return;

        _watchers.Remove(watcher);
        UpdateTimestamp();
        AddDomainEvent(new WatcherRemovedFromTaskEvent(Id, userId));
    }

    // === SUPPORT ENTITIES ===

    public Comment AddComment(string content, Guid authorId)
    {
        // Normalize inputs
        content = content?.Trim() ?? string.Empty;
        
        // Validate
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
        // Normalize inputs
        fileName = fileName?.Trim() ?? string.Empty;
        fileUrl = fileUrl?.Trim() ?? string.Empty;
        fileType = string.IsNullOrWhiteSpace(fileType?.Trim()) ? null : fileType.Trim();
        
        // Validate
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
        // Normalize inputs
        description = string.IsNullOrWhiteSpace(description?.Trim()) ? null : description.Trim();
        
        // Validate
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
        // Normalize input
        title = title?.Trim() ?? string.Empty;
        
        // Validate
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Checklist title cannot be empty.", nameof(title));

        var checklist = Checklist.Create(title, Id);
        _checklists.Add(checklist);
        UpdateTimestamp();
        AddDomainEvent(new ChecklistAddedToTaskEvent(Id, checklist.Id, title));
        return checklist;
    }

    // === TAG MANAGEMENT ===

    public void AddTag(Guid tagId)
    {
        ValidateGuid(tagId, nameof(tagId));
            
        if (_tags.Any(t => t.TagId == tagId)) return; // Already tagged

        var tag = new ProjectTaskTag(Id, tagId);
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
        // Use existing values if not provided
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
        AddDomainEvent(new TaskMovedEvent(Id, oldListId, newListId, oldFolderId, newFolderId, oldSpaceId, newSpaceId.Value));
    }

    // === PRIVATE CASCADE METHODS ===
    
    private void CascadeVisibilityToSubtasks(Visibility newVisibility)
    {
        // Only cascade if making more restrictive (Public -> Private)
        if (newVisibility == Visibility.Private)
        {
            foreach (var subtask in _subtasks.Where(s => s.Visibility == Visibility.Public))
            {
                subtask.ChangeVisibility(Visibility.Private);
            }
        }
    }

    // === VALIDATION HELPER METHODS ===
    
    private static void ValidateBasicInfo(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Task name cannot be empty.", nameof(name));
        if (name.Length > 200)
            throw new ArgumentException("Task name cannot exceed 200 characters.", nameof(name));
        if (description?.Length > 1000)
            throw new ArgumentException("Task description cannot exceed 1000 characters.", nameof(description));
    }
    
    private static void ValidateSchedule(DateTime? startDate, DateTime? dueDate)
    {
        if (startDate.HasValue && dueDate.HasValue && startDate > dueDate)
            throw new ArgumentException("Start date cannot be after due date.", nameof(startDate));
    }
    
    private static void ValidateEstimation(int? storyPoints, long? timeEstimateSeconds)
    {
        if (storyPoints.HasValue && storyPoints < 0)
            throw new ArgumentOutOfRangeException(nameof(storyPoints), "Story points cannot be negative.");
        if (timeEstimateSeconds.HasValue && timeEstimateSeconds < 0)
            throw new ArgumentOutOfRangeException(nameof(timeEstimateSeconds), "Time estimate cannot be negative.");
    }
    
    private static void ValidateGuid(Guid guid, string parameterName)
    {
        if (guid == Guid.Empty)
            throw new ArgumentException($"{parameterName} cannot be empty.", parameterName);
    }

    // === QUERY HELPERS ===

    public bool IsAssignedTo(Guid userId) => _assignees.Any(a => a.UserId == userId);
    
    public bool IsWatchedBy(Guid userId) => _watchers.Any(w => w.UserId == userId);
    
    public bool HasTag(Guid tagId) => _tags.Any(t => t.TagId == tagId);
    
    public TimeSpan GetTotalLoggedTime() => 
        TimeSpan.FromTicks(_timeLogs.Sum(tl => tl.Duration.Ticks));
    
    public bool IsOverdue() => DueDate.HasValue && DueDate < DateTime.UtcNow && !IsCompleted;
    
    public bool CanUserManage(Guid userId) => userId == CreatorId;
    
    public bool CanUserEdit(Guid userId) => CanUserManage(userId) || IsAssignedTo(userId);
    
    public int GetSubtaskCount() => _subtasks.Count;
    
    public int GetCompletedSubtaskCount() => _subtasks.Count(s => s.IsCompleted);
    
    public double GetSubtaskCompletionPercentage() => 
        _subtasks.Count == 0 ? 0 : (double)GetCompletedSubtaskCount() / _subtasks.Count * 100;
}