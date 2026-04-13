using Domain.Common;
using Domain.Entities.ProjectEntities.ValueObject;
using Domain.Enums;
using Domain.Enums.RelationShip;
using Domain.Enums.Workspace;
using Domain.Events.Membership;
using Domain.Exceptions;

namespace Domain.Entities;

public sealed class ProjectWorkspace : Entity
{
    public string Name { get; private set; } = null!;
    public string Slug { get; private set; } = null!;
    public string? Description { get; private set; }
    public string JoinCode { get; private set; } = null!;
    public Customization Customization { get; private set; } = Customization.CreateDefault();
    public Theme Theme { get; private set; } = Theme.Dark;
    public bool StrictJoin { get; private set; } = false;
    public bool IsArchived { get; private set; }

    private readonly List<WorkspaceMember> _members = new();
    public IReadOnlyCollection<WorkspaceMember> Members => _members.AsReadOnly();

    private ProjectWorkspace() { }

    private ProjectWorkspace(Guid id, string name, string slug, string? description, string joinCode, Customization customization, Theme theme, bool strictJoin, Guid creatorId)
    {
        Id = id;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Slug = slug ?? throw new ArgumentNullException(nameof(slug));
        Description = string.IsNullOrWhiteSpace(description) ? null : description;
        JoinCode = joinCode ?? throw new ArgumentNullException(nameof(joinCode));
        Customization = customization ?? Customization.CreateDefault();
        Theme = theme;
        StrictJoin = strictJoin;
        CreatorId = creatorId;
        IsArchived = false;
    }

    public static ProjectWorkspace Create(string name, string slug, string? description, string? joinCode, Customization? customization, Guid creatorId, Theme theme = Theme.Dark, bool strictJoin = false)
    {
        ValidateBasicInfo(name, slug, description);
        
        var workspace = new ProjectWorkspace(
            Guid.NewGuid(), 
            name.Trim(),
            slug.Trim().ToLowerInvariant(),
            string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            string.IsNullOrWhiteSpace(joinCode) ? Guid.NewGuid().ToString("N")[..8].ToUpperInvariant() : joinCode.Trim(),
            customization ?? Customization.CreateDefault(), 
            theme, 
            strictJoin, 
            creatorId);
        
        // Add creator as the first owner
        var owner = WorkspaceMember.CreateOwner(creatorId, workspace.Id, creatorId);
        workspace._members.Add(owner);
        
        workspace.AddDomainEvent(new Domain.Events.Workspace.CreatedWorkspaceEvent(creatorId, workspace.Id));
        return workspace;
    }

    #region Member Management

    public void AddMember(Guid userId, Role role, Guid actorId, string? joinMethod = "Manual")
    {
        EnsureNotArchived();
        
        var newMember = WorkspaceMember.Create(userId, this.Id, role, MembershipStatus.Active, actorId, joinMethod);
        _members.Add(newMember);
        
        AddDomainEvent(new WorkspaceMembersAddedBulkEvent(Id, new[] { new AddedMemberRecord(userId, role) }));
        UpdateTimestamp();
    }

    public void AddMembers(IEnumerable<(Guid UserId, Role Role)> memberSpecs, Guid actorId)
    {
        EnsureNotArchived();
        
        var addedRecords = new List<AddedMemberRecord>();
        foreach (var spec in memberSpecs)
        {
            var newMember = WorkspaceMember.Create(spec.UserId, this.Id, spec.Role, MembershipStatus.Active, actorId, "Bulk");
            _members.Add(newMember);
            addedRecords.Add(new AddedMemberRecord(spec.UserId, spec.Role));
        }

        if (addedRecords.Any())
        {
            AddDomainEvent(new WorkspaceMembersAddedBulkEvent(Id, addedRecords));
            UpdateTimestamp();
        }
    }

    public void RemoveMembers(IEnumerable<Guid> userIds)
    {
        EnsureNotArchived();
        
        var idsToRemove = userIds.ToList();
        var removedCount = _members.RemoveAll(m => idsToRemove.Contains(m.UserId));

        if (removedCount > 0)
        {
            AddDomainEvent(new WorkspaceMembersRemovedBulkEvent(Id, idsToRemove));
            UpdateTimestamp();
        }
    }

    public void TransferOwnership(Guid newOwnerId, Guid actorId)
    {
        EnsureNotArchived();

        if (CreatorId == newOwnerId) return;

        var newOwnerMember = _members.FirstOrDefault(m => m.UserId == newOwnerId);
        if (newOwnerMember == null || newOwnerMember.Status != MembershipStatus.Active)
        {
            throw new MembershipException("New owner must be an active member of the workspace.");
        }

        var currentOwnerId = CreatorId ?? throw new BusinessRuleException("Workspace has no active owner.");
        var currentOwnerMember = _members.FirstOrDefault(m => m.UserId == currentOwnerId);

        // Technical transfer
        CreatorId = newOwnerId;
        
        // Role transfer
        newOwnerMember.UpdateRole(Role.Owner);
        currentOwnerMember?.UpdateRole(Role.Admin);

        UpdateTimestamp();
    }

    #endregion

    #region Update Methods

    public void UpdateBasicInfo(string? name, string? slug, string? description)
    {
        EnsureNotArchived();

        var candidateName = name?.Trim() ?? Name;
        var candidateSlug = slug?.Trim().ToLowerInvariant() ?? Slug;
        var candidateDescription = description?.Trim() ?? Description;

        if (candidateName == Name && candidateSlug == Slug && candidateDescription == Description) return;

        ValidateBasicInfo(candidateName, candidateSlug, candidateDescription);

        Name = candidateName;
        Slug = candidateSlug;
        Description = string.IsNullOrWhiteSpace(candidateDescription) ? null : candidateDescription;

        UpdateTimestamp();
    }

    public void UpdateCustomization(string? color, string? icon)
    {
        EnsureNotArchived();
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
        EnsureNotArchived();
        if (Theme == theme) return;
        Theme = theme;
        UpdateTimestamp();
    }

    public void UpdateStrictJoin(bool strictJoin)
    {
        EnsureNotArchived();
        if (StrictJoin == strictJoin) return;
        StrictJoin = strictJoin;
        UpdateTimestamp();
    }

    public void Delete()
    {
        SoftDelete();
        AddDomainEvent(new Domain.Events.Workspace.WorkspaceDeletedEvent(Id));
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
        EnsureNotArchived();
        JoinCode = GenerateRandomCode();
        UpdateTimestamp();
    }

    #endregion

    #region Private Helpers

    private void EnsureNotArchived()
    {
        if (IsArchived) throw new BusinessRuleException("Cannot modify an archived workspace.");
    }

    private static string GenerateRandomCode(int length = 8)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Range(0, length)
            .Select(_ => chars[Random.Shared.Next(chars.Length)])
            .ToArray());
    }

    private static void ValidateBasicInfo(string name, string slug, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new BusinessRuleException("Workspace name cannot be empty.");
        if (name.Length > 100)
            throw new BusinessRuleException("Workspace name cannot exceed 100 characters.");
        if (string.IsNullOrWhiteSpace(slug))
            throw new BusinessRuleException("Slug cannot be empty.");
        if (description?.Length > 500)
            throw new BusinessRuleException("Workspace description cannot exceed 500 characters.");
    }

    #endregion
}
