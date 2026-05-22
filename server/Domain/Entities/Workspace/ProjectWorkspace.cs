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

    #endregion

    #region Update Methods

    public void Update(
        string? name = null,
        string? slug = null,
        string? description = null,
        string? color = null,
        string? icon = null,
        bool? strictJoin = null)
    {
        EnsureNotArchived();
        bool updated = false;

        if (name != null && Name != name) { Name = name; updated = true; }
        if (slug != null && Slug != slug) { Slug = slug; updated = true; }
        if (description != null && Description != description) { Description = description; updated = true; }
        if (color != null && Color != color) { Color = color; updated = true; }
        if (icon != null && Icon != icon) { Icon = icon; updated = true; }
        if (strictJoin != null && StrictJoin != strictJoin) { StrictJoin = strictJoin.Value; updated = true; }

        if (updated) UpdateTimestamp();
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
