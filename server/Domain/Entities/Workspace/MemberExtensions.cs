using System;
using System.Linq;
using Domain.Entities.Relationship;
using Domain.Enums;
using Domain.Enums.RelationShip;

namespace Domain.Entities;

public static class MemberExtensions
{
    public static IQueryable<WorkspaceMember> ById(this IQueryable<WorkspaceMember> query, Guid id) => 
        query.Where(m => m.Id == id);

    public static IQueryable<WorkspaceMember> ByWorkspace(this IQueryable<WorkspaceMember> query, Guid workspaceId) =>
        query.Where(m => m.ProjectWorkspaceId == workspaceId);

    public static IQueryable<WorkspaceMember> ByUser(this IQueryable<WorkspaceMember> query, Guid userId) =>
        query.Where(m => m.UserId == userId);

    public static IQueryable<WorkspaceMember> ByMember(this IQueryable<WorkspaceMember> query, Guid workspaceId, Guid userId) =>
        query.Where(m => m.ProjectWorkspaceId == workspaceId && m.UserId == userId);

    public static IQueryable<WorkspaceMember> WhereActive(this IQueryable<WorkspaceMember> query) =>
        query.Where(m => m.Status == MembershipStatus.Active && m.DeletedAt == null);

    public static IQueryable<WorkspaceMember> WhereNotDeleted(this IQueryable<WorkspaceMember> query) =>
        query.Where(m => m.DeletedAt == null);

    public static IQueryable<WorkspaceMember> InRoles(this IQueryable<WorkspaceMember> query, params Role[] roles) =>
        query.Where(m => roles.Contains(m.Role));
}
