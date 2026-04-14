using System;
using Domain.Entities;
using Domain.Common;
using Domain.Enums;
using Domain.Enums.RelationShip;
using Domain.Events.Membership;

namespace Domain.Entities;

public class WorkspaceMember : Entity
{
    public Guid UserId { get; private set; }
    public virtual User User { get; private set; } = null!;
    public Guid ProjectWorkspaceId { get; private set; }
    public Role Role { get; private set; }
    public MembershipStatus Status { get; private set; } // Pending, Active, Invited, Suspended
    public bool IsPinned { get; private set; }
    public DateTimeOffset? JoinedAt { get; private set; }
    public DateTimeOffset? SuspendedAt { get; private set; }
    public Guid? SuspendedBy { get; private set; }
    public string? JoinMethod { get; private set; } = string.Empty; // "Invite", "Request", "Code"
    public WorkspaceMember(Guid userId, Guid projectWorkspaceId, Role role, MembershipStatus status, Guid? creatorId, string? joinMethod)
    {
        UserId = userId;
        ProjectWorkspaceId = projectWorkspaceId;
        Role = role;
        Status = status;
        IsPinned = false;
        CreatorId = creatorId;
        JoinedAt = status == MembershipStatus.Active ? DateTimeOffset.UtcNow : null;
        JoinMethod = joinMethod;
    }

    public static WorkspaceMember Create(Guid userId, Guid workspaceId, Role role, MembershipStatus status, Guid createdBy, string? joinMethod)
      => new(userId, workspaceId, role, status, createdBy, joinMethod);

    public static WorkspaceMember CreateOwner(Guid userId, Guid projectWorkspaceId, Guid createdBy)
        => new(userId, projectWorkspaceId, Role.Owner, MembershipStatus.Active, createdBy, "Created");

    public static List<WorkspaceMember> AddBulk(List<(Guid UserId, Role Role, MembershipStatus Status, string? JoinMethod)> memberSpecs, Guid projectWorkspaceId, Guid createdBy)
    {
        return memberSpecs
            .Select(spec => new WorkspaceMember(
                spec.UserId,
                projectWorkspaceId,
                spec.Role,
                spec.Status,
                createdBy,
                spec.JoinMethod))
            .ToList();
    }

    public void ApproveMembership()
    {
        Status = MembershipStatus.Active;
        JoinedAt = DateTimeOffset.UtcNow;
    }

    public void SuspendMembership(Guid suspenderId)
    {
        Status = MembershipStatus.Suspended;
        SuspendedAt = DateTimeOffset.UtcNow;
        SuspendedBy = suspenderId;
    }

    public void UpdateStatus(MembershipStatus newStatus) => Status = newStatus;

    public void UpdateRole(Role newRole)
    {
        Role = newRole;
        UpdateTimestamp();
    }

    public void UpdateMembershipDetails(Role? newRole, MembershipStatus? newStatus)
    {
        if (newRole.HasValue)
        {
            UpdateRole(newRole.Value);
        }

        if (newStatus.HasValue)
        {
            UpdateStatus(newStatus.Value);
        }
    }

    public void RestoreMember()
    {
        DeletedAt = null;
        Status = MembershipStatus.Active;
        JoinedAt = DateTimeOffset.UtcNow;
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
            JoinedAt = null;
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
}
