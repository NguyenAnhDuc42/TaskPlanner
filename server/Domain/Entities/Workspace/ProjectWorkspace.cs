using Domain.Common;
using Domain.Enums;
using Domain.Enums.RelationShip;
using Domain.Exceptions;

namespace Domain.Entities;

public sealed class ProjectWorkspace : Entity
{
    public string Name { get; private set; } = null!;
    public string Slug { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public string JoinCode { get; private set; } = null!;
    public string Color { get; private set; } = "#FFFFFF";
    public string? Icon { get; private set; }
    public bool StrictJoin { get; private set; } = false;
    public bool IsArchived { get; private set; }
    public bool IsInitialized { get; private set; } = false;

    public void MarkAsInitialized()
    {
        if (IsInitialized) return;
        IsInitialized = true;
        UpdateTimestamp();
    }

    private readonly List<WorkspaceMember> _members = new();
    public IReadOnlyCollection<WorkspaceMember> Members => _members.AsReadOnly();

    private ProjectWorkspace() { }

    private ProjectWorkspace(Guid id, string name, string slug, string description, string joinCode, string color, string? icon, bool strictJoin, Guid creatorId)
        : base(id)
    {
        Name = name;
        Slug = slug;
        Description = description;
        JoinCode = joinCode;
        Color = color;
        Icon = icon;
        StrictJoin = strictJoin;
        IsArchived = false;
        
        // Audit is initialized in base constructor
        InitializeAudit(creatorId);
    }

    public static ProjectWorkspace Create(string name, string slug, string description, string? joinCode, string? color, string? icon, Guid creatorId, Theme theme = Theme.Dark, bool strictJoin = false)
    {
        var workspace = new ProjectWorkspace(
            Guid.NewGuid(), 
            name,
            slug,
            description,
            string.IsNullOrWhiteSpace(joinCode) ? Guid.NewGuid().ToString("N")[..8] : joinCode,
            color ?? "#FFFFFF",
            icon,
            strictJoin, 
            creatorId);
        
        // Add creator as the first owner with their chosen theme
        var owner = WorkspaceMember.CreateOwner(creatorId, workspace.Id, creatorId, theme);
        workspace._members.Add(owner);
        
        return workspace;
    }

    #region Member Management

    public void AddMember(Guid userId, Role role, Guid actorId, string? joinMethod = "Manual", Theme theme = Theme.Dark)
    {
        EnsureNotArchived();
        
        var newMember = WorkspaceMember.Create(userId, this.Id, role, MembershipStatus.Active, actorId, joinMethod, theme);
        _members.Add(newMember);
        
        UpdateTimestamp();
    }

    public void AddMembers(IEnumerable<(Guid UserId, Role Role)> memberSpecs, Guid actorId, Theme theme = Theme.Dark)
    {
        EnsureNotArchived();
        
        foreach (var spec in memberSpecs)
        {
            var newMember = WorkspaceMember.Create(spec.UserId, this.Id, spec.Role, MembershipStatus.Active, actorId, "Bulk", theme);
            _members.Add(newMember);
        }

        UpdateTimestamp();
    }

    public void RemoveMembers(IEnumerable<Guid> userIds)
    {
        EnsureNotArchived();
        
        var idsToRemove = userIds.ToList();
        var removedCount = _members.RemoveAll(m => idsToRemove.Contains(m.UserId));

        if (removedCount > 0)
        {
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

        // We can't easily "transfer" CreatorId
        // but we can update the ownership role.
        
        newOwnerMember.UpdateRole(Role.Owner);
        var currentOwnerMember = _members.FirstOrDefault(m => m.UserId == CreatorId);
        currentOwnerMember?.UpdateRole(Role.Admin);

        UpdateTimestamp();
    }

    #endregion

    #region Update Methods

    public void UpdateName(string name)
    {
        EnsureNotArchived();
        Name = name;
        UpdateTimestamp();
    }

    public void UpdateSlug(string slug)
    {
        EnsureNotArchived();
        if (Slug == slug) return;
        Slug = slug;
        UpdateTimestamp();
    }

    public void UpdateDescription(string description)
    {
        EnsureNotArchived();
        Description = description;
        UpdateTimestamp();
    }

    public void UpdateColor(string color)
    {
        EnsureNotArchived();
        if (Color == color) return;
        Color = color;
        UpdateTimestamp();
    }

    public void UpdateIcon(string? icon)
    {
        EnsureNotArchived();
        if (Icon == icon) return;
        Icon = icon;
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
        JoinCode = Guid.NewGuid().ToString("N")[..8];
        UpdateTimestamp();
    }

    #endregion

    #region Private Helpers

    private void EnsureNotArchived()
    {
        if (IsArchived) throw new BusinessRuleException("Cannot modify an archived workspace.");
    }

    #endregion
}
