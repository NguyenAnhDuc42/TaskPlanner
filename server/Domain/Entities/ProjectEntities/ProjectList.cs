using Domain.Common;
using Domain.Entities.Relationship;
using Domain.Enums;
using Domain.Events.ListEvents;
using static Domain.Common.ColorValidator;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Domain.Entities.ProjectEntities;

public class ProjectList : Aggregate
{
    private const int MAX_BATCH_SIZE = 500; // Safety limit for list-level batch ops

    public Guid ProjectWorkspaceId { get; private set; }
    public Guid ProjectSpaceId { get; private set; }
    public Guid? ProjectFolderId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public int? OrderIndex { get; private set; }
    public Visibility Visibility { get; private set; }
    public Guid CreatorId { get; private set; }
    public bool IsArchived { get; private set; }

    public DateTime? StartDate { get; private set; }
    public DateTime? DueDate { get; private set; }

    private readonly List<UserProjectList> _members = new();
    public IReadOnlyCollection<UserProjectList> Members => _members.AsReadOnly();

    private readonly List<ProjectTask> _tasks = new();
    public IReadOnlyCollection<ProjectTask> Tasks => _tasks.AsReadOnly();

    private ProjectList() { } // For EF Core

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
        AddDomainEvent(new ListBasicInfoUpdatedEvent(Id, oldName, name, oldDescription, description));
    }

    public void ChangeVisibility(Visibility newVisibility)
    {
        if (Visibility == newVisibility) return;

        var oldVisibility = Visibility;
        Visibility = newVisibility;
        UpdateTimestamp();
        AddDomainEvent(new ListVisibilityChangedEvent(Id, oldVisibility, newVisibility));

        CascadeVisibilityToTasks(newVisibility);
    }

    public void SetDateRange(DateTime? startDate, DateTime? dueDate)
    {
        if (startDate.HasValue && dueDate.HasValue && startDate > dueDate)
            throw new ArgumentException("Start date cannot be later than due date.", nameof(startDate));

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

        ArchiveAllTasks();
    }

    public void Unarchive()
    {
        if (!IsArchived) return;

        IsArchived = false;
        UpdateTimestamp();
        AddDomainEvent(new ListUnarchivedEvent(Id));

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

    // === MEMBERSHIP ===

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

    // === TASK MANAGEMENT ===

    public ProjectTask CreateTask(string name, string? description, Priority priority = Priority.Medium,
        DateTime? startDate = null, DateTime? dueDate = null, Visibility visibility = Visibility.Public)
    {
        if (IsArchived)
            throw new InvalidOperationException("Cannot create tasks in an archived list.");

        name = name?.Trim() ?? string.Empty;
        description = string.IsNullOrWhiteSpace(description?.Trim()) ? null : description.Trim();

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

        _tasks.Remove(task);

        UpdateTimestamp();
        AddDomainEvent(new TaskMovedFromListEvent(Id, taskId, targetListId));
    }

    public void ReceiveTaskFromList(ProjectTask task, Guid sourceListId)
    {
        if (task == null)
            throw new ArgumentNullException(nameof(task));

        if (IsArchived)
            throw new InvalidOperationException("Cannot move tasks to an archived list.");

        task.MoveToList(Id);

        var newOrderIndex = _tasks.Count;
        task.UpdateOrderIndex(newOrderIndex);

        _tasks.Add(task);

        UpdateTimestamp();
        AddDomainEvent(new TaskMovedToListEvent(Id, task.Id, sourceListId));
    }

    // === BULK OPERATIONS ===

    public void ArchiveAllTasks()
    {
        var tasksToArchive = _tasks.Where(t => !t.IsArchived).ToList();
        if (!tasksToArchive.Any()) return;

        if (tasksToArchive.Count > MAX_BATCH_SIZE)
            throw new InvalidOperationException($"Batch too large. Reduce size below {MAX_BATCH_SIZE} or run as smaller batches.");

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

        if (tasksToUnarchive.Count > MAX_BATCH_SIZE)
            throw new InvalidOperationException($"Batch too large. Reduce size below {MAX_BATCH_SIZE} or run as smaller batches.");

        foreach (var task in tasksToUnarchive)
        {
            task.Unarchive();
        }

        UpdateTimestamp();
        AddDomainEvent(new AllTasksUnarchivedInListEvent(Id));
    }

    /// <summary>
    /// Completing all tasks needs workspace-level default "completed" status resolution.
    /// This is orchestration requiring external domain service. Moved to application handler.
    /// </summary>
    public void CompleteAllTasks()
    {
        // TODO: MOVE_TO_HANDLER: Completing all tasks requires workspace-level default completed status.
        throw new InvalidOperationException("This method was moved to application handler. See TODO: MOVE_TO_HANDLER: CompleteAllTasks");
    }

    public void AssignAllTasksToUser(Guid userId)
    {
        ValidateGuid(userId, nameof(userId));

        var tasksToAssign = _tasks.Where(t => !t.IsAssignedTo(userId) && !t.IsArchived).ToList();
        if (!tasksToAssign.Any()) return;

        if (tasksToAssign.Count > MAX_BATCH_SIZE)
            throw new InvalidOperationException($"Batch too large. Reduce size below {MAX_BATCH_SIZE} or run in smaller batches.");

        foreach (var task in tasksToAssign)
        {
            task.AssignUser(userId);
        }

        UpdateTimestamp();
        AddDomainEvent(new AllTasksAssignedToUserInListEvent(Id, userId));
    }

    public void UpdateAllTasksPriority(Priority newPriority)
    {
        var tasksToUpdate = _tasks.Where(t => t.Priority != newPriority && !t.IsArchived).ToList();
        if (!tasksToUpdate.Any()) return;

        if (tasksToUpdate.Count > MAX_BATCH_SIZE)
            throw new InvalidOperationException($"Batch too large. Reduce size below {MAX_BATCH_SIZE} or run in smaller batches.");

        foreach (var task in tasksToUpdate)
        {
            task.ChangePriority(newPriority);
        }

        UpdateTimestamp();
        AddDomainEvent(new AllTasksPriorityUpdatedInListEvent(Id, newPriority));
    }

    // === PRIVATE CASCADE ===

    private void CascadeVisibilityToTasks(Visibility newVisibility)
    {
        if (newVisibility == Visibility.Private)
        {
            foreach (var task in _tasks.Where(t => t.Visibility == Visibility.Public))
            {
                task.ChangeVisibility(Visibility.Private);
            }
        }
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

    private void ValidateTaskCreation(string name, DateTime? startDate, DateTime? dueDate)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Task name cannot be empty.", nameof(name));

        if (startDate.HasValue && dueDate.HasValue && startDate > dueDate)
            throw new ArgumentException("Task start date cannot be later than due date.", nameof(startDate));
    }

    private static void ValidateGuid(Guid id, string paramName)
    {
        if (id == Guid.Empty) throw new ArgumentException("Guid cannot be empty.", paramName);
    }
}
