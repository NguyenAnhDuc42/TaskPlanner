using System;
using System.ComponentModel.DataAnnotations;
using Domain.Common;
using Domain.Enums;
using Domain.Enums.RelationShip;

namespace Domain.Entities.Relationship;

public class WorkspaceMember : Composite
{
    public Guid UserId { get; private set; }
    public Guid ProjectWorkspaceId { get; private set; }
    public Role Role { get; private set; } // Only here
    public MembershipStatus Status { get; private set; } // Pending, Active, Invited, Suspended
    public Guid CreatedBy { get; private set; } // who invited
    public DateTimeOffset JoinedAt { get; private set; }
    public DateTimeOffset? ApprovedAt { get; private set; }
    public Guid? ApprovedBy { get; private set; }
    public DateTimeOffset? SuspendedAt { get; private set; }
    public Guid? SuspendedBy { get; private set; }
    public string? JoinMethod { get; private set; } = string.Empty; // "Invite", "Request", "Code"
    private WorkspaceMember() { } // EF

    public WorkspaceMember(Guid userId, Guid projectWorkspaceId, Role role, MembershipStatus status, Guid createdBy, string? joinMethod)
    {
        UserId = userId;
        ProjectWorkspaceId = projectWorkspaceId;
        Role = role;
        Status = status;
        CreatedBy = createdBy;
        JoinedAt = DateTimeOffset.UtcNow;
        JoinMethod = joinMethod;
    }

    public static WorkspaceMember CreateOwner(Guid userId, Guid projectWorkspaceId, Guid createdBy)
        => new(userId, projectWorkspaceId, Role.Owner, MembershipStatus.Active, createdBy, "Created");
    public static WorkspaceMember AddMember(Guid userId, Guid workspaceId, Role role, MembershipStatus status, Guid createdBy, string? joinMethod)
        => new(userId, workspaceId, role, status, createdBy, joinMethod);

    public void UpdateStatus(MembershipStatus newStatus) => Status = newStatus;
    public void ApproveMembership(Guid approverId)
    {
        Status = MembershipStatus.Active;
        ApprovedAt = DateTimeOffset.UtcNow;
        ApprovedBy = approverId;
    }
    public void SuspendMembership(Guid suspenderId)
    {
        Status = MembershipStatus.Suspended;
        SuspendedAt = DateTimeOffset.UtcNow;
        SuspendedBy = suspenderId;
    }
    public void UpdateRole(Role newRole) => Role = newRole;

}

