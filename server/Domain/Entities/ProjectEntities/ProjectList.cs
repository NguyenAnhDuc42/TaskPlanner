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
    public Guid ProjectWorkspaceId { get; private set; }
    public Guid ProjectSpaceId { get; private set; }
    public Guid? ProjectFolderId { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public int? OrderIndex { get; private set; }
    public Visibility Visibility { get; private set; }
    public Guid CreatorId { get; private set; }
    public bool IsArchived { get; private set; }

    public DateTimeOffset? StartDate { get; private set; }
    public DateTimeOffset? DueDate { get; private set; }

    private readonly List<UserProjectList> _members = new();
    public IReadOnlyCollection<UserProjectList> Members => _members.AsReadOnly();

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
    }

    public void SetDateRange(DateTimeOffset? startDate, DateTimeOffset? dueDate)
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
    }

    public void Unarchive()
    {
        if (!IsArchived) return;

        IsArchived = false;
        UpdateTimestamp();
        AddDomainEvent(new ListUnarchivedEvent(Id));
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

    private static void ValidateGuid(Guid id, string paramName)
    {
        if (id == Guid.Empty) throw new ArgumentException("Guid cannot be empty.", paramName);
    }
}
