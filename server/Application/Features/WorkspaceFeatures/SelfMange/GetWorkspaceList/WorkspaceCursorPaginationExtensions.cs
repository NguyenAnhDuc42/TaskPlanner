using System;
using Application.Common.Filters;
using Application.Helper;
using Domain.Entities.ProjectEntities;

namespace Application.Features.WorkspaceFeatures.SelfMange.GetWorkspaceList;

public static class WorkspaceCursorPaginationExtensions
{
    public static IQueryable<ProjectWorkspace> ApplyFilter(this IQueryable<ProjectWorkspace> query, WorkspaceFilter filter, Guid currentUserId)
    {
        if (!string.IsNullOrEmpty(filter.Name)) query = query.Where(w => w.Name.Contains(filter.Name));
        if (filter.Owned) query = query.Where(w => w.CreatorId == currentUserId);
        if (filter.isArchived) query = query.Where(w => w.IsArchived == filter.isArchived);
        if (filter.Variant != null) query = query.Where(w => w.Variant == filter.Variant);
        return query;
    }

    public static IQueryable<ProjectWorkspace> ApplyCursor(this IQueryable<ProjectWorkspace> query, CursorPaginationRequest pagination,CursorHelper cursorHelper)
    {
        if (string.IsNullOrEmpty(pagination.Cursor)) return query;
        var cursorData = cursorHelper.DecodeCursor(pagination.Cursor);
        if (cursorData?.Values == null || !cursorData.Values.ContainsKey("Timestamp")) return query;
        if (!DateTime.TryParse(cursorData.Values["Timestamp"].ToString(), out var timestamp)) return query;
        return pagination.Direction == SortDirection.Ascending ? query.Where(w => w.UpdatedAt > timestamp) : query.Where(w => w.UpdatedAt < timestamp);
    }
    public static IQueryable<ProjectWorkspace> ApplySort(this IQueryable<ProjectWorkspace> query, CursorPaginationRequest pagination)
    {
        return pagination.Direction == SortDirection.Ascending
        ? query.OrderBy(w => w.UpdatedAt).ThenBy(w => w.Id)
        : query.OrderByDescending(w => w.UpdatedAt).ThenByDescending(w => w.Id);
    }
}
