using System;
using Application.Common.Filters;
using Application.Helper;
using Domain.Entities.ProjectEntities;

namespace Application.Features.WorkspaceFeatures.GetWorkspaceList;

public static class WorkspaceCursorPaginationExtensions
{
    public static IQueryable<ProjectWorkspace> ApplyFilter(this IQueryable<ProjectWorkspace> query, WorkspaceFilter filter, Guid currentUserId)
    {
        if (!string.IsNullOrEmpty(filter.Name)) query = query.Where(w => w.Name.Contains(filter.Name));
        if (filter.Owned.HasValue && filter.Owned.Value) query = query.Where(w => w.CreatorId == currentUserId);
        if (filter.isArchived.HasValue && filter.isArchived.Value) query = query.Where(w => w.IsArchived == filter.isArchived);
        if (filter.Variant != null) query = query.Where(w => w.Variant == filter.Variant);
        return query;
    }

    public static IQueryable<ProjectWorkspace> ApplyCursor(this IQueryable<ProjectWorkspace> query, CursorPaginationRequest pagination, CursorHelper cursorHelper)
    {
        if (string.IsNullOrEmpty(pagination.Cursor)) return query;

        var cursorData = cursorHelper.DecodeCursor(pagination.Cursor);

        if (cursorData?.Values == null) return query;

        if (!DateTimeOffset.TryParse(cursorData.Values["Timestamp"]?.ToString(), out var timestampOffset)) return query;
        if (!Guid.TryParse(cursorData.Values["Id"]?.ToString(), out var id)) return query;

        return pagination.Direction == SortDirection.Ascending
        ? query.Where(w =>
            w.UpdatedAt > timestampOffset ||
            (w.UpdatedAt == timestampOffset && w.Id.CompareTo(id) > 0))
        : query.Where(w =>
            w.UpdatedAt < timestampOffset ||
            (w.UpdatedAt == timestampOffset && w.Id.CompareTo(id) < 0));
    }
    public static IQueryable<ProjectWorkspace> ApplySort(this IQueryable<ProjectWorkspace> query, CursorPaginationRequest pagination)
    {
        return pagination.Direction == SortDirection.Ascending
        ? query.OrderBy(w => w.UpdatedAt).ThenBy(w => w.Id)
        : query.OrderByDescending(w => w.UpdatedAt).ThenByDescending(w => w.Id);
    }
}
