using Application.Common.Filters;
using Application.Helper;
using Domain.Entities.ProjectEntities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Application.Features.DashboardFeatures.GetWidgetList;

public static class GetWidgetListCursorPagination
{
    public static IQueryable<Widget> ApplyFilter(this IQueryable<Widget> query, Guid dashboardId)
    {
        return query.Where(w => w.DashboardId == dashboardId && w.DeletedAt == null);
    }

    public static IQueryable<Widget> ApplyCursor(this IQueryable<Widget> query, CursorPaginationRequest pagination, CursorHelper cursorHelper)
    {
        if (string.IsNullOrEmpty(pagination.Cursor)) return query;

        var cursorData = cursorHelper.DecodeCursor(pagination.Cursor);
        if (cursorData?.Values == null || !cursorData.Values.ContainsKey("Timestamp")) return query;

        if (!DateTimeOffset.TryParse(cursorData.Values["Timestamp"].ToString(), out var timestampOffset)) return query;

        return pagination.Direction == SortDirection.Ascending
            ? query.Where(w => w.UpdatedAt > timestampOffset)
            : query.Where(w => w.UpdatedAt < timestampOffset);
    }

    public static IQueryable<Widget> ApplySort(this IQueryable<Widget> query, CursorPaginationRequest pagination)
    {
        return pagination.Direction == SortDirection.Ascending
            ? query.OrderBy(w => w.UpdatedAt).ThenBy(w => w.Id)
            : query.OrderByDescending(w => w.UpdatedAt).ThenByDescending(w => w.Id);
    }
}
