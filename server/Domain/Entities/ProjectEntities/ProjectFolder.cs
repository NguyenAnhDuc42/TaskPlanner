using Domain.Common;
using Domain.Entities.Relationship;
using Domain.Enums;
using Domain.Events.FolderEvents;
using Domain.Services.UsageChecker;
using static Domain.Common.ColorValidator;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Domain.Entities.ProjectEntities;

public class ProjectFolder : Aggregate
{
    private const int MAX_BATCH_SIZE = 500; // Safety limit for folder-level batch ops

    public Guid ProjectWorkspaceId { get; private set; }
    public Guid ProjectSpaceId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public int? OrderIndex { get; private set; }
    public Visibility Visibility { get; private set; }
    public bool IsArchived { get; private set; }
    public Guid CreatorId { get; private set; }

    private readonly List<UserProjectFolder> _members = new();
    public IReadOnlyCollection<UserProjectFolder> Members => _members.AsReadOnly();

    private readonly List<ProjectList> _lists = new();
    public IReadOnlyCollection<ProjectList> Lists => _lists.AsReadOnly();

    private ProjectFolder() { } // For EF Core

    internal ProjectFolder(Guid id, Guid projectWorkspaceId, Guid projectSpaceId, string name,
        string? description, Visibility visibility, int orderIndex, Guid creatorId)
    {
        Id = id;
        ProjectWorkspaceId = projectWorkspaceId;
        ProjectSpaceId = projectSpaceId;
        Name = name;
        Description = description;
        Visibility = visibility;
        OrderIndex = orderIndex;
        CreatorId = creatorId;
    }

    // === VALIDATION METHOD ===

    public static void ValidateForCreation(string name, string? description, string color,
        Guid projectWorkspaceId, Guid projectSpaceId, Guid creatorId)
    {
        ValidateBasicInfo(name, description);
        ValidateColor(color);
        ValidateGuid(projectWorkspaceId, nameof(projectWorkspaceId));
        ValidateGuid(projectSpaceId, nameof(projectSpaceId));
        ValidateGuid(creatorId, nameof(creatorId));
    }

    // === SELF MANAGEMENT METHODS ===

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
        AddDomainEvent(new FolderBasicInfoUpdatedEvent(Id, oldName, name, oldDescription, description));
    }

    public void ChangeVisibility(Visibility newVisibility)
    {
        if (Visibility == newVisibility) return;

        var oldVisibility = Visibility;
        Visibility = newVisibility;
        UpdateTimestamp();
        AddDomainEvent(new FolderVisibilityChangedEvent(Id, oldVisibility, newVisibility));

        // CASCADE: Update child lists where applicable
        CascadeVisibilityToLists(newVisibility);
    }

    internal void UpdateOrderIndex(int newOrderIndex)
    {
        if (newOrderIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(newOrderIndex), "Order index cannot be negative.");

        if (OrderIndex == newOrderIndex) return;

        OrderIndex = newOrderIndex;
        UpdateTimestamp();
    }

    // === MEMBERSHIP MANAGEMENT ===

    public void AddMember(Guid userId)
    {
        ValidateGuid(userId, nameof(userId));

        if (_members.Any(m => m.UserId == userId))
            throw new InvalidOperationException("User is already a member of this folder.");

        var member = UserProjectFolder.Create(userId, Id);
        _members.Add(member);
        UpdateTimestamp();
        AddDomainEvent(new MemberAddedToFolderEvent(Id, userId));

        // Cascade membership to child lists pre-check to avoid exceptions
        foreach (var list in _lists.Where(l => l.Visibility == Visibility.Private))
        {
            if (!list.Members.Any(m => m.UserId == userId))
                list.AddMember(userId);
        }
    }

    public void RemoveMember(Guid userId)
    {
        if (userId == CreatorId)
            throw new InvalidOperationException("Cannot remove folder creator from folder.");

        var member = _members.FirstOrDefault(m => m.UserId == userId);
        if (member == null)
            throw new InvalidOperationException("User is not a member of this folder.");

        _members.Remove(member);
        UpdateTimestamp();
        AddDomainEvent(new MemberRemovedFromFolderEvent(Id, userId));

        // Cascade removal to child lists
        foreach (var list in _lists.Where(l => l.Visibility == Visibility.Private))
        {
            var exists = list.Members.Any(m => m.UserId == userId);
            if (exists)
                list.RemoveMember(userId);
        }
    }

    public void Archive()
    {
        if (IsArchived) return;

        IsArchived = true;
        UpdateTimestamp();
        AddDomainEvent(new FolderArchivedEvent(Id));

        // BATCH_RULE: archive child lists (safety enforced)
        ArchiveAllLists();
    }

    public void Unarchive()
    {
        if (!IsArchived) return;

        IsArchived = false;
        UpdateTimestamp();
        AddDomainEvent(new FolderUnarchivedEvent(Id));

        UnarchiveAllLists();
    }

    // === CHILD ENTITY MANAGEMENT ===

    public ProjectList CreateList(string name, string? description, string color, Visibility visibility)
    {
        name = name?.Trim() ?? string.Empty;
        description = string.IsNullOrWhiteSpace(description?.Trim()) ? null : description.Trim();
        color = color?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("List name cannot be empty.", nameof(name));
        ValidateColor(color);

        if (_lists.Any(l => l.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"A list with the name '{name}' already exists in this folder.");

        var orderIndex = _lists.Count;
        var list = new ProjectList(Guid.NewGuid(), ProjectWorkspaceId, ProjectSpaceId, Id,
            name, description, visibility, orderIndex, CreatorId);
        _lists.Add(list);

        UpdateTimestamp();
        AddDomainEvent(new ListCreatedInFolderEvent(Id, list.Id, name, CreatorId));
        return list;
    }

    /// <summary>
    /// RemoveList requires an IListUsageChecker to ensure it has no tasks preventing removal.
    /// </summary>
    public void RemoveList(Guid listId)
    {
        throw new InvalidOperationException("This operation requires a domain-service check. Use RemoveList(Guid listId, IListUsageChecker usageChecker).");
    }

    public async Task RemoveList(Guid listId, IListUsageChecker usageChecker)
    {
        if (usageChecker == null) throw new ArgumentNullException(nameof(usageChecker));

        var list = _lists.FirstOrDefault(l => l.Id == listId);
        if (list == null)
            throw new InvalidOperationException("List not found in this folder.");

        if (await usageChecker.IsInUseAsync(listId))
            throw new InvalidOperationException("Cannot remove list because it contains tasks according to the provided checker.");

        _lists.Remove(list);
        UpdateTimestamp();
        AddDomainEvent(new ListRemovedFromFolderEvent(Id, listId, list.Name));
    }

    public void ReorderLists(List<Guid> listIds)
    {
        if (listIds.Count != _lists.Count)
            throw new ArgumentException("Must provide all list IDs for reordering.", nameof(listIds));

        var allListsExist = listIds.All(id => _lists.Any(l => l.Id == id));
        if (!allListsExist)
            throw new ArgumentException("One or more list IDs are invalid.", nameof(listIds));

        for (int i = 0; i < listIds.Count; i++)
        {
            var list = _lists.First(l => l.Id == listIds[i]);
            list.UpdateOrderIndex(i);
        }

        UpdateTimestamp();
        AddDomainEvent(new ListsReorderedInFolderEvent(Id, listIds));
    }

    // === BULK OPERATIONS (BATCH_RULE) ===

    public void ArchiveAllLists()
    {
        var toArchive = _lists.Where(l => !l.IsArchived).ToList();
        if (!toArchive.Any()) return;

        if (toArchive.Count > MAX_BATCH_SIZE)
            throw new InvalidOperationException($"Batch too large. Reduce size below {MAX_BATCH_SIZE} or run in smaller batches.");

        foreach (var list in toArchive)
        {
            list.Archive();
        }

        UpdateTimestamp();
        AddDomainEvent(new AllListsArchivedInFolderEvent(Id));
    }

    public void UnarchiveAllLists()
    {
        var toUnarchive = _lists.Where(l => l.IsArchived).ToList();
        if (!toUnarchive.Any()) return;

        if (toUnarchive.Count > MAX_BATCH_SIZE)
            throw new InvalidOperationException($"Batch too large. Reduce size below {MAX_BATCH_SIZE} or run in smaller batches.");

        foreach (var list in toUnarchive)
        {
            list.Unarchive();
        }

        UpdateTimestamp();
        AddDomainEvent(new AllListsUnarchivedInFolderEvent(Id));
    }

    // === ATTACH/DETACH ===

    internal void AttachList(ProjectList list)
    {
        if (list == null)
            throw new ArgumentNullException(nameof(list));
        if (list.ProjectWorkspaceId != ProjectWorkspaceId || list.ProjectSpaceId != ProjectSpaceId)
            throw new InvalidOperationException("List belongs to different workspace or space.");
        if (_lists.Any(l => l.Id == list.Id))
            return;

        if (_lists.Any(l => l.Name.Equals(list.Name, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"A list with the name '{list.Name}' already exists in this folder.");

        _lists.Add(list);
        UpdateTimestamp();
    }

    internal void DetachList(Guid listId)
    {
        var list = _lists.FirstOrDefault(l => l.Id == listId);
        if (list == null) return;

        _lists.Remove(list);
        UpdateTimestamp();
    }

    // === PRIVATE CASCADE METHODS ===

    private void CascadeVisibilityToLists(Visibility newVisibility)
    {
        if (newVisibility == Visibility.Private)
        {
            foreach (var list in _lists.Where(l => l.Visibility == Visibility.Public))
            {
                list.ChangeVisibility(Visibility.Private);
            }
        }
    }

    // === VALIDATION HELPERS ===

    private static void ValidateBasicInfo(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Folder name cannot be empty.", nameof(name));
        if (name.Length > 100)
            throw new ArgumentException("Folder name cannot exceed 100 characters.", nameof(name));
        if (description?.Length > 500)
            throw new ArgumentException("Folder description cannot exceed 500 characters.", nameof(description));
    }

    private static void ValidateColor(string color)
    {
        if (string.IsNullOrWhiteSpace(color))
            throw new ArgumentException("Folder color cannot be empty.", nameof(color));
        if (!IsValidColorCode(color))
            throw new ArgumentException("Invalid color format.", nameof(color));
    }

    private static void ValidateGuid(Guid id, string paramName)
    {
        if (id == Guid.Empty) throw new ArgumentException("Guid cannot be empty.", paramName);
    }
}
