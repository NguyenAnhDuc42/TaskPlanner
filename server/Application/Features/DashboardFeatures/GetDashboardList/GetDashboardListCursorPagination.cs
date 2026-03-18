using Application.Common.Filters;
using Application.Helper;
using Domain.Entities.ProjectEntities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Features.DashboardManagement.GetDashboardList;

public static class GetDashboardListCursorPagination
{
    public static IQueryable<Dashboard> ApplyFilter(this IQueryable<Dashboard> query, DashboardFilter filter, Guid currentUserId)
    {
        if (!string.IsNullOrEmpty(filter.name)) query = query.Where(w => w.Name.Contains(filter.name));
        if (filter.owned == true) query = query.Where(w => w.CreatorId == currentUserId);
        if (filter.createdAt.HasValue) query = query.Where(w => w.CreatedAt >= filter.createdAt.Value);
        return query;
    }

    public static IQueryable<Dashboard> ApplyCursor(this IQueryable<Dashboard> query, CursorPaginationRequest pagination, CursorHelper cursorHelper)
    {
        if (string.IsNullOrEmpty(pagination.Cursor)) return query;
        var cursorData = cursorHelper.DecodeCursor(pagination.Cursor);
        if (cursorData?.Values == null || !cursorData.Values.ContainsKey("Timestamp")) return query;
        if (!DateTimeOffset.TryParse(cursorData.Values["Timestamp"].ToString(), out var timestampOffset)) return query;
        return pagination.Direction == SortDirection.Ascending
        ? query.Where(w => w.UpdatedAt > timestampOffset)
        : query.Where(w => w.UpdatedAt < timestampOffset);
    }
    public static IQueryable<Dashboard> ApplySort(this IQueryable<Dashboard> query, CursorPaginationRequest pagination)
    {
        return pagination.Direction == SortDirection.Ascending
        ? query.OrderBy(w => w.UpdatedAt).ThenBy(w => w.Id)
        : query.OrderByDescending(w => w.UpdatedAt).ThenByDescending(w => w.Id);
    }
}
