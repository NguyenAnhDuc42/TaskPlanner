
using Domain.Common;
using Domain.Entities.ProjectEntities.ValueObject;
using Domain.Entities.Relationship;
using Domain.Enums;
using Domain.Enums.RelationShip;
using Domain.Enums.Workspace;
using Domain.Events;
using Domain.Events.Membership;

namespace Domain.Entities.ProjectEntities;

public sealed class ProjectWorkspace : Entity
{
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public string JoinCode { get; private set; } = null!;
    public Customization Customization { get; private set; } = Customization.CreateDefault();
    public Theme Theme { get; private set; } = Theme.System;
    public WorkspaceVariant Variant { get; private set; } = WorkspaceVariant.Team;
    public bool StrictJoin { get; private set; } = false;
    public bool IsArchived { get; private set; }
    public long NextItemOrder { get; private set; }

    private readonly List<WorkspaceMember> _members = new();
    public IReadOnlyCollection<WorkspaceMember> Members => _members.AsReadOnly();

    private ProjectWorkspace() { }

    private ProjectWorkspace(Guid id, string name, string? description, string joinCode, Customization customization, Theme theme, WorkspaceVariant variant, bool strictJoin, Guid creatorId, long nextItemOrder)
    {
        Id = id;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = string.IsNullOrWhiteSpace(description) ? null : description;
        JoinCode = joinCode ?? throw new ArgumentNullException(nameof(joinCode));
        Customization = customization ?? Customization.CreateDefault();
        Theme = theme;
        Variant = variant;
        StrictJoin = strictJoin;
        CreatorId = creatorId;
        IsArchived = false;
        NextItemOrder = nextItemOrder;
    }

    public static ProjectWorkspace Create(string name, string? description, string? joinCode, Customization? customization, Guid creatorId, Theme theme = Theme.System, WorkspaceVariant variant = WorkspaceVariant.Team, bool strictJoin = false)
    {
        var workspace = new ProjectWorkspace(Guid.NewGuid(), name?.Trim() ?? throw new ArgumentNullException(nameof(name)),
        string.IsNullOrWhiteSpace(description) ? null : description?.Trim(),
        string.IsNullOrWhiteSpace(joinCode) ? Guid.NewGuid().ToString("N")[..8].ToUpperInvariant() : joinCode.Trim(),
        customization ?? Customization.CreateDefault(), theme, variant, strictJoin, creatorId, 10_000_000L);
        workspace.AddMember(creatorId, Role.Owner, MembershipStatus.Active, creatorId, null);
        return workspace;
    }

    public long GetNextItemOrderAndIncrement()
    {
        var currentOrder = NextItemOrder;
        NextItemOrder += 10_000_000L;
        return currentOrder;
    }

    #region Member Management

    public void AddMember(Guid userId, Role role, MembershipStatus status, Guid createdBy, string? joinMethod)
    {
        if (_members.Any(m => m.UserId == userId))
        {
            throw new InvalidOperationException("User is already a member of this workspace.");
        }
        var newMember = WorkspaceMember.Create(userId, this.Id, role, status, createdBy, joinMethod);
        _members.Add(newMember);
        
        AddDomainEvent(new WorkspaceMembersAddedBulkEvent(Id, new[] { new AddedMemberRecord(userId, role) }));
        
        UpdateTimestamp();
    }

    public void AddMembers(List<(Guid UserId, Role Role, MembershipStatus Status, string? JoinMethod)> memberSpecs, Guid createdBy)
    {
        var newMembers = WorkspaceMember.AddBulk(memberSpecs, this.Id, createdBy);

        var changed = false;
        var addedRecords = new List<AddedMemberRecord>();
        foreach (var member in newMembers)
        {
            if (!_members.Any(m => m.UserId == member.UserId))
            {
                _members.Add(member);
                addedRecords.Add(new AddedMemberRecord(member.UserId, member.Role));
                changed = true;
            }
        }

        if (changed)
        {
            AddDomainEvent(new WorkspaceMembersAddedBulkEvent(Id, addedRecords));
            UpdateTimestamp();
        }
    }

    public void RemoveMembers(IEnumerable<Guid> userIds)
    {
        var idsToRemove = userIds.ToList();
        var removedCount = _members.RemoveAll(m => idsToRemove.Contains(m.UserId));

        if (removedCount > 0)
        {
            AddDomainEvent(new WorkspaceMembersRemovedBulkEvent(Id, idsToRemove));
            UpdateTimestamp();
        }
    }

    public void TransferOwnership(Guid newOwnerId)
    {
        if (CreatorId == newOwnerId) return;
        if (!CreatorId.HasValue)
            throw new InvalidOperationException("Workspace has no owner to transfer from.");

        var oldOwnerId = CreatorId.Value;
        var newOwnerMember = _members.FirstOrDefault(m => m.UserId == newOwnerId);
        var oldOwnerMember = _members.FirstOrDefault(m => m.UserId == oldOwnerId);

        if (newOwnerMember == null)
            throw new InvalidOperationException("New owner must be a member of the workspace.");

        CreatorId = newOwnerId;
        newOwnerMember.UpdateMembershipDetails(Role.Owner, newOwnerMember.Status);
        oldOwnerMember?.UpdateMembershipDetails(Role.Admin, oldOwnerMember.Status);

        UpdateTimestamp();
    }

    #endregion

    #region Update Methods

    public void UpdateBasicInfo(string? name, string? description)
    {
        var candidateName = name is null 
            ? Name 
            : (name.Trim() == string.Empty 
                ? throw new ArgumentException("Name cannot be empty.", nameof(name)) 
                : name.Trim());
                
        var candidateDescription = description is null 
            ? Description 
            : (string.IsNullOrWhiteSpace(description.Trim()) ? null : description.Trim());

        ValidateBasicInfo(candidateName, candidateDescription);

        var changed = false;
        if (candidateName != Name) { Name = candidateName; changed = true; }
        if (candidateDescription != Description) { Description = candidateDescription; changed = true; }

        if (changed) UpdateTimestamp();
    }

    public void UpdateCustomization(string? color, string? icon)
    {
        if (color is null && icon is null) return;

        var newColor = color?.Trim() ?? Customization.Color;
        var newIcon = icon?.Trim() ?? Customization.Icon;
        var newCustomization = Customization.Create(newColor, newIcon);

        if (!newCustomization.Equals(Customization))
        {
            Customization = newCustomization;
            UpdateTimestamp();
        }
    }

    public void UpdateTheme(Theme theme)
    {
        if (Theme == theme) return;
        Theme = theme;
        UpdateTimestamp();
    }

    public void UpdateVariant(WorkspaceVariant variant)
    {
        if (Variant == variant) return;
        Variant = variant;
        UpdateTimestamp();
    }

    public void UpdateStrictJoin(bool strictJoin)
    {
        if (StrictJoin == strictJoin) return;
        StrictJoin = strictJoin;
        UpdateTimestamp();
    }

    public void Archive()
    {
        if (IsArchived) return;
        IsArchived = true;
        UpdateTimestamp();
    }

    public void Unarchive()
    {
        if (!IsArchived) return;
        IsArchived = false;
        UpdateTimestamp();
    }

    public void RegenerateJoinCode()
    {
        JoinCode = GenerateRandomCode();
        UpdateTimestamp();
    }

    #endregion

    #region Private Helpers

    private static string GenerateRandomCode(int length = 6)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Range(0, length)
            .Select(_ => chars[Random.Shared.Next(chars.Length)])
            .ToArray());
    }

    private static void ValidateBasicInfo(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Workspace name cannot be empty.", nameof(name));
        if (name.Length > 100)
            throw new ArgumentException("Workspace name cannot exceed 100 characters.", nameof(name));
        if (description?.Length > 500)
            throw new ArgumentException("Workspace description cannot exceed 500 characters.", nameof(description));
    }

    #endregion
}