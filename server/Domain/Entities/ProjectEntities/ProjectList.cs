using Domain.Common;
using Domain.Entities.Relationship;
using Domain.Enums;
using Domain.Events.ListEvents;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Domain.Entities.ProjectEntities;

public class ProjectList : Aggregate
{
    public Guid ProjectWorkspaceId { get; private set; }
    public Guid ProjectSpaceId { get; private set; }
    public Guid? ProjectFolderId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public int? OrderIndex { get; private set; } // Nullable for proper ordering
    public Visibility Visibility { get; private set; }
    public Guid CreatorId { get; private set; }
    public bool IsArchived { get; private set; }

    // Date constraints for the list
    public DateTime? StartDate { get; private set; }
    public DateTime? DueDate { get; private set; }

    // Members (for private/restricted lists) - no roles, just access
    private readonly List<UserProjectList> _members = new();
    public IReadOnlyCollection<UserProjectList> Members => _members.AsReadOnly();

    // Child entities - actual tasks, not just IDs
    private readonly List<ProjectTask> _tasks = new();
    public IReadOnlyCollection<ProjectTask> Tasks => _tasks.AsReadOnly();

    // Constructors
    private ProjectList() { } // For EF Core

    // Internal constructor - only called by parent space
    internal ProjectList(Guid id, Guid projectWorkspaceId, Guid projectSpaceId, Guid? projectFolderId,
        string name, string? description, Visibility visibility, int orderIndex, Guid creatorId)
    {
        Id = id;
        ProjectWorkspaceId = projectWorkspaceId;
        ProjectSpaceId = projectSpaceId;
        ProjectFolderId = projectFolderId;
        Name = name;
        Description = description;
        Visibility = visibility;
        OrderIndex = orderIndex;
        CreatorId = creatorId;
        IsArchived = false;
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
        AddDomainEvent(new ListBasicInfoUpdatedEvent(Id, oldName, name, oldDescription, description));
    }


    public void ChangeVisibility(Visibility newVisibility)
    {
        // Check for changes first
        if (Visibility == newVisibility) return;

        var oldVisibility = Visibility;
        Visibility = newVisibility;
        UpdateTimestamp();
        AddDomainEvent(new ListVisibilityChangedEvent(Id, oldVisibility, newVisibility));

        // CASCADE: Update all child tasks to match list visibility if they were public
        CascadeVisibilityToTasks(newVisibility);
    }

    public void SetDateRange(DateTime? startDate, DateTime? dueDate)
    {
        // Validate first
        if (startDate.HasValue && dueDate.HasValue && startDate > dueDate)
            throw new ArgumentException("Start date cannot be later than due date.", nameof(startDate));

        // Check for changes
        if (StartDate == startDate && DueDate == dueDate) return;

        var oldStartDate = StartDate;
        var oldDueDate = DueDate;
        StartDate = startDate;
        DueDate = dueDate;
        UpdateTimestamp();
        AddDomainEvent(new ListDateRangeUpdatedEvent(Id, oldStartDate, startDate, oldDueDate, dueDate));
    }

    public void Archive()
    {
        if (IsArchived) return;

        IsArchived = true;
        UpdateTimestamp();
        AddDomainEvent(new ListArchivedEvent(Id));

        // CASCADE: Archive all child tasks
        ArchiveAllTasks();
    }

    public void Unarchive()
    {
        if (!IsArchived) return;

        IsArchived = false;
        UpdateTimestamp();
        AddDomainEvent(new ListUnarchivedEvent(Id));

        // CASCADE: Unarchive all child tasks
        UnarchiveAllTasks();
    }

    internal void UpdateOrderIndex(int newOrderIndex)
    {
        if (OrderIndex == newOrderIndex) return;

        OrderIndex = newOrderIndex;
        UpdateTimestamp();
    }

    internal void MoveToFolder(Guid? newFolderId)
    {
        if (ProjectFolderId == newFolderId) return;

        var oldFolderId = ProjectFolderId;
        ProjectFolderId = newFolderId;
        UpdateTimestamp();
        AddDomainEvent(new ListMovedToFolderEvent(Id, oldFolderId, newFolderId));
    }
    internal void MoveToSpace(Guid newSpaceId)
    {
        if (ProjectSpaceId == newSpaceId) return;

        var oldSpaceId = ProjectSpaceId;
        ProjectSpaceId = newSpaceId;
        UpdateTimestamp();
        AddDomainEvent(new ListMovedToFolderEvent(Id, oldSpaceId, newSpaceId));
    }

    // === MEMBERSHIP MANAGEMENT ===

    public void AddMember(Guid userId)
    {
        ValidateGuid(userId, nameof(userId));

        if (_members.Any(m => m.UserId == userId))
            throw new InvalidOperationException("User is already a member of this list.");

        var member = UserProjectList.Create(userId, Id);
        _members.Add(member);
        UpdateTimestamp();
        AddDomainEvent(new MemberAddedToListEvent(Id, userId));
    }

    public void RemoveMember(Guid userId)
    {
        if (userId == CreatorId)
            throw new InvalidOperationException("Cannot remove list creator from list.");

        var member = _members.FirstOrDefault(m => m.UserId == userId);
        if (member == null)
            throw new InvalidOperationException("User is not a member of this list.");

        _members.Remove(member);
        UpdateTimestamp();
        AddDomainEvent(new MemberRemovedFromListEvent(Id, userId));
    }

    // === CHILD ENTITY MANAGEMENT (TASKS) ===

    public ProjectTask CreateTask(string name, string? description, Priority priority = Priority.Medium,
        DateTime? startDate = null, DateTime? dueDate = null, Visibility visibility = Visibility.Public)
    {
        if (IsArchived)
            throw new InvalidOperationException("Cannot create tasks in an archived list.");

        // Normalize inputs
        name = name?.Trim() ?? string.Empty;
        description = string.IsNullOrWhiteSpace(description?.Trim()) ? null : description.Trim();

        // Validate
        ValidateTaskCreation(name, startDate, dueDate);

        var orderIndex = _tasks.Count;
        var task = new ProjectTask(Guid.NewGuid(), ProjectWorkspaceId, ProjectSpaceId, ProjectFolderId, Id,
            name, description, priority, startDate, dueDate, visibility, orderIndex, CreatorId);
        _tasks.Add(task);

        UpdateTimestamp();
        AddDomainEvent(new TaskCreatedInListEvent(Id, task.Id, name, CreatorId));
        return task;
    }

    public void RemoveTask(Guid taskId)
    {
        var task = _tasks.FirstOrDefault(t => t.Id == taskId);
        if (task == null)
            throw new InvalidOperationException("Task not found in this list.");

        _tasks.Remove(task);
        UpdateTimestamp();
        AddDomainEvent(new TaskRemovedFromListEvent(Id, taskId, task.Name));
    }

    public void ReorderTasks(List<Guid> taskIds)
    {
        if (taskIds.Count != _tasks.Count)
            throw new ArgumentException("Must provide all task IDs for reordering.", nameof(taskIds));

        var allTasksExist = taskIds.All(id => _tasks.Any(t => t.Id == id));
        if (!allTasksExist)
            throw new ArgumentException("One or more task IDs are invalid.", nameof(taskIds));

        for (int i = 0; i < taskIds.Count; i++)
        {
            var task = _tasks.First(t => t.Id == taskIds[i]);
            task.UpdateOrderIndex(i);
        }

        UpdateTimestamp();
        AddDomainEvent(new TasksReorderedInListEvent(Id, taskIds));
    }

    public void MoveTaskToList(Guid taskId, Guid targetListId)
    {
        var task = _tasks.FirstOrDefault(t => t.Id == taskId);
        if (task == null)
            throw new InvalidOperationException("Task not found in this list.");

        // Remove from this list
        _tasks.Remove(task);

        // The target list will handle adding the task
        // This is typically coordinated by an application service

        UpdateTimestamp();
        AddDomainEvent(new TaskMovedFromListEvent(Id, taskId, targetListId));
    }

    public void ReceiveTaskFromList(ProjectTask task, Guid sourceListId)
    {
        if (task == null)
            throw new ArgumentNullException(nameof(task));

        if (IsArchived)
            throw new InvalidOperationException("Cannot move tasks to an archived list.");

        // Update task's list reference
        task.MoveToList(Id);

        // Set new order index
        var newOrderIndex = _tasks.Count;
        task.UpdateOrderIndex(newOrderIndex);

        // Add to this list
        _tasks.Add(task);

        UpdateTimestamp();
        AddDomainEvent(new TaskMovedToListEvent(Id, task.Id, sourceListId));
    }

    // === BULK OPERATIONS (MISSING HIERARCHICAL METHODS) ===

    public void ArchiveAllTasks()
    {
        var tasksToArchive = _tasks.Where(t => !t.IsArchived).ToList();
        if (!tasksToArchive.Any()) return;

        foreach (var task in tasksToArchive)
        {
            task.Archive();
        }

        UpdateTimestamp();
        AddDomainEvent(new AllTasksArchivedInListEvent(Id));
    }

    public void UnarchiveAllTasks()
    {
        var tasksToUnarchive = _tasks.Where(t => t.IsArchived).ToList();
        if (!tasksToUnarchive.Any()) return;

        foreach (var task in tasksToUnarchive)
        {
            task.Unarchive();
        }

        UpdateTimestamp();
        AddDomainEvent(new AllTasksUnarchivedInListEvent(Id));
    }

    public void CompleteAllTasks()
    {
        var tasksToComplete = _tasks.Where(t => !t.IsCompleted && !t.IsArchived).ToList();
        if (!tasksToComplete.Any()) return;

        foreach (var task in tasksToComplete)
        {
            // This would need a default "completed" status from the workspace
            // task.UpdateStatus(completedStatusId, true);
        }

        UpdateTimestamp();
        AddDomainEvent(new AllTasksCompletedInListEvent(Id));
    }

    public void AssignAllTasksToUser(Guid userId)
    {
        ValidateGuid(userId, nameof(userId));

        var tasksToAssign = _tasks.Where(t => !t.IsAssignedTo(userId) && !t.IsArchived).ToList();
        if (!tasksToAssign.Any()) return;

        foreach (var task in tasksToAssign)
        {
            try
            {
                task.AssignUser(userId);
            }
            catch (InvalidOperationException)
            {
                // Task might already be assigned - ignore
            }
        }

        UpdateTimestamp();
        AddDomainEvent(new AllTasksAssignedToUserInListEvent(Id, userId));
    }

    public void UpdateAllTasksPriority(Priority newPriority)
    {
        var tasksToUpdate = _tasks.Where(t => t.Priority != newPriority && !t.IsArchived).ToList();
        if (!tasksToUpdate.Any()) return;

        foreach (var task in tasksToUpdate)
        {
            task.ChangePriority(newPriority);
        }

        UpdateTimestamp();
        AddDomainEvent(new AllTasksPriorityUpdatedInListEvent(Id, newPriority));
    }

    // === PRIVATE CASCADE METHODS ===

    private void CascadeVisibilityToTasks(Visibility newVisibility)
    {
        // Only cascade if making more restrictive (Public -> Private)
        if (newVisibility == Visibility.Private)
        {
            foreach (var task in _tasks.Where(t => t.Visibility == Visibility.Public))
            {
                task.ChangeVisibility(Visibility.Private);
            }
        }
    }

    // === VALIDATION HELPER METHODS ===

    private static void ValidateBasicInfo(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("List name cannot be empty.", nameof(name));
        if (name.Length > 100)
            throw new ArgumentException("List name cannot exceed 100 characters.", nameof(name));
        if (description?.Length > 500)
            throw new ArgumentException("List description cannot exceed 500 characters.", nameof(description));
    }

    private static void ValidateColor(string color)
    {
        if (string.IsNullOrWhiteSpace(color))
            throw new ArgumentException("List color cannot be empty.", nameof(color));
        if (!IsValidColorCode(color))
            throw new ArgumentException("Invalid color format.", nameof(color));
    }

    private void ValidateTaskCreation(string name, DateTime? startDate, DateTime? dueDate)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Task name cannot be empty.", nameof(name));

        if (startDate.HasValue && dueDate.HasValue && startDate > dueDate)
            throw new ArgumentException("Task start date cannot be later than due date.", nameof(startDate));

        // Validate task dates are within list date range if set
        if (StartDate.HasValue && startDate.HasValue && startDate < StartDate)
            throw new ArgumentException("Task start date cannot be before list start date.", nameof(startDate));
        if (DueDate.HasValue && dueDate.HasValue && dueDate > DueDate)
            throw new ArgumentException("Task due date cannot be after list due date.", nameof(dueDate));
    }

    private static void ValidateGuid(Guid guid, string parameterName)
    {
        if (guid == Guid.Empty)
            throw new ArgumentException($"{parameterName} cannot be empty.", parameterName);
    }

    private static bool IsValidColorCode(string color) =>
        !string.IsNullOrWhiteSpace(color) &&
        (color.StartsWith("#") && (color.Length == 7 || color.Length == 4));

    // === QUERY HELPER METHODS ===

    public bool HasMember(Guid userId) => _members.Any(m => m.UserId == userId);

    public bool CanUserAccess(Guid userId) =>
        Visibility == Visibility.Public ||
        userId == CreatorId ||
        HasMember(userId);

         
    public bool CanUserManage(Guid userId) => userId == CreatorId;
    
    public IEnumerable<ProjectTask> GetOrderedTasks() => 
        _tasks.OrderBy(t => t.OrderIndex ?? int.MaxValue).ThenBy(t => t.CreatedAt);
        
    public IEnumerable<ProjectTask> GetTasksByStatus(Guid statusId) => 
        _tasks.Where(t => t.StatusId == statusId);
        
    public IEnumerable<ProjectTask> GetOverdueTasks() => 
        _tasks.Where(t => t.DueDate.HasValue && t.DueDate < DateTime.UtcNow && !t.IsCompleted);
        
    public IEnumerable<ProjectTask> GetCompletedTasks() => 
        _tasks.Where(t => t.IsCompleted);
        
    public int GetTaskCount() => _tasks.Count;
    
    public int GetCompletedTaskCount() => _tasks.Count(t => t.IsCompleted);
    
    public double GetCompletionPercentage() => 
        _tasks.Count == 0 ? 0 : (double)GetCompletedTaskCount() / _tasks.Count * 100;
}