using Domain.Common;
using Domain.Enums;
using Domain.Enums.RelationShip;

namespace Domain.Entities;

public class WorkspaceMember : Entity
{
    public Guid UserId { get; private set; }
    public virtual User User { get; private set; } = null!;
    public Guid ProjectWorkspaceId { get; private set; }
    public Role Role { get; private set; }
    public MembershipStatus Status { get; private set; }
    public DateTimeOffset? JoinedAt { get; private set; }
    public DateTimeOffset? SuspendedAt { get; private set; }
    public Guid? SuspendedBy { get; private set; }
    public bool IsPinned { get; private set; }
    public Theme Theme { get; private set; }
    public string? JoinMethod { get; private set; } = string.Empty; 

    private WorkspaceMember() { } 

    public WorkspaceMember(Guid userId, Guid projectWorkspaceId, Role role, MembershipStatus status, Guid? creatorId, string? joinMethod, Theme theme = Theme.Dark)
        : base(Guid.NewGuid())
    {
        UserId = userId;
        ProjectWorkspaceId = projectWorkspaceId;
        Role = role;
        Status = status;
        JoinedAt = status == MembershipStatus.Active ? DateTimeOffset.UtcNow : null;
        SuspendedAt = null;
        SuspendedBy = null;
        IsPinned = false;
        Theme = theme;
        JoinMethod = joinMethod;

        // Audit is initialized in base constructor
        InitializeAudit(creatorId);
    }

    public static WorkspaceMember Create(Guid userId, Guid workspaceId, Role role, MembershipStatus status, Guid createdBy, string? joinMethod, Theme theme = Theme.Dark)
      => new(userId, workspaceId, role, status, createdBy, joinMethod, theme);

    public static WorkspaceMember CreateOwner(Guid userId, Guid projectWorkspaceId, Guid createdBy, Theme theme = Theme.Dark)
        => new(userId, projectWorkspaceId, Role.Owner, MembershipStatus.Active, createdBy, "Created", theme);

    public void ApproveMembership()
    {
        Status = MembershipStatus.Active;
        JoinedAt = DateTimeOffset.UtcNow;
        UpdateTimestamp();
    }

    public void SuspendMembership(Guid suspenderId)
    {
        SuspendedAt = DateTimeOffset.UtcNow;
        SuspendedBy = suspenderId;
        Status = MembershipStatus.Suspended;
        UpdateTimestamp();
    }

    public void UpdateRole(Role newRole)
    {
        if (Role == newRole) return;
        Role = newRole;
        UpdateTimestamp();
    }

    public void UpdateStatus(MembershipStatus newStatus) 
    {
        if (Status == newStatus) return;
        Status = newStatus;
        UpdateTimestamp();
    }

    public void RestoreMember()
    {
        DeletedAt = null;
        Status = MembershipStatus.Active;
        JoinedAt = DateTimeOffset.UtcNow;
        SuspendedAt = null;
        SuspendedBy = null;
    }

    public void SetPinned(bool isPinned)
    {
        if (IsPinned == isPinned) return;
        IsPinned = isPinned;
        UpdateTimestamp();
    }

    public void JoinByCode(bool strictJoin)
    {
        if (Status == MembershipStatus.Suspended)
            throw new InvalidOperationException("Suspended members cannot join this workspace.");

        if (strictJoin)
        {
            Status = MembershipStatus.Pending;
        }
        else
        {
            Status = MembershipStatus.Active;
            JoinedAt = DateTimeOffset.UtcNow;
        }

        UpdateTimestamp();
    }

    public void RestoreForJoinByCode(bool strictJoin)
    {
        DeletedAt = null;
        JoinByCode(strictJoin);
    }

    public void UpdateTheme(Theme theme)
    {
        if (Theme == theme) return;
        Theme = theme;
        UpdateTimestamp();
    }
}
