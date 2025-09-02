using System;
using Application.Common.Filters;
using Domain.Entities.ProjectEntities;
using Infrastructure.Helper;

namespace Infrastructure.Data.Repositories.Extensions;

public static class WorkspaceQueryExtension
{
    public static IQueryable<ProjectWorkspace> ApplyFilter(this IQueryable<ProjectWorkspace> query, WorkspaceFilter filter, Guid currentUserId)
    {
        if (!string.IsNullOrWhiteSpace(filter.Name))
            query = query.Where(w => w.Name.Contains(filter.Name));

        if (!string.IsNullOrWhiteSpace(filter.Icon))
            query = query.Where(w => w.Icon == filter.Icon);

        if (filter.Visibility.HasValue)
            query = query.Where(w => w.Visibility == filter.Visibility.Value);

        if (filter.Owned)
            query = query.Where(w => w.CreatorId == currentUserId);

        if (filter.isArchived)
            query = query.Where(w => w.IsArchived);

        return query;
    }
    public static IQueryable<ProjectWorkspace> ApplySorting(this IQueryable<ProjectWorkspace> query, string sortBy, SortDirection direction)
    {
        return (sortBy, direction) switch
        {
            ("Name", SortDirection.Ascending) => query.OrderBy(w => w.Name),
            ("Name", SortDirection.Descending) => query.OrderByDescending(w => w.Name),
            ("CreatedAt", SortDirection.Ascending) => query.OrderBy(w => w.CreatedAt),
            ("CreatedAt", SortDirection.Descending) => query.OrderByDescending(w => w.CreatedAt),
            ("UpdatedAt", SortDirection.Ascending) => query.OrderBy(w => w.UpdatedAt),
            ("UpdatedAt", SortDirection.Descending) => query.OrderByDescending(w => w.UpdatedAt),
            _ => query.OrderByDescending(w => w.UpdatedAt) // default
        };
    }

    public static IQueryable<ProjectWorkspace> ApplyCursor(this IQueryable<ProjectWorkspace> query, CursorPaginationRequest pagination, CursorHelper cursorHelper)
    {
        if (!string.IsNullOrEmpty(pagination.Cursor))
        {
            var cursorData = cursorHelper.DecodeCursor(pagination.Cursor);
            if (cursorData?.Values.TryGetValue(pagination.SortBy, out var value) == true)
            {
                query = ApplyCursorFilter(query, pagination.SortBy, pagination.Direction, value);
            }
        }

        return query.Take(pagination.PageSize + 1); // fetch 1 extra to check HasNextPage
    }

    private static IQueryable<ProjectWorkspace> ApplyCursorFilter(IQueryable<ProjectWorkspace> query, string sortBy, SortDirection direction, object value)
    {
        return (sortBy, direction) switch
        {
            ("Name", SortDirection.Ascending) => query.Where(w => String.Compare(w.Name, value.ToString(), StringComparison.Ordinal) > 0),
            ("Name", SortDirection.Descending) => query.Where(w => String.Compare(w.Name, value.ToString(), StringComparison.Ordinal) < 0),

            ("CreatedAt", SortDirection.Ascending) => query.Where(w => w.CreatedAt > Convert.ToDateTime(value)),
            ("CreatedAt", SortDirection.Descending) => query.Where(w => w.CreatedAt < Convert.ToDateTime(value)),

            ("UpdatedAt", SortDirection.Ascending) => query.Where(w => w.UpdatedAt > Convert.ToDateTime(value)),
            ("UpdatedAt", SortDirection.Descending) => query.Where(w => w.UpdatedAt < Convert.ToDateTime(value)),

            _ => query
        };
    }

    public static string? BuildNextCursor(this IEnumerable<ProjectWorkspace> items, CursorPaginationRequest pagination, CursorHelper cursorHelper)
    {
        var lastItem = items.LastOrDefault();
        if (lastItem == null) return null;

        var values = new Dictionary<string, object>
        {
            { pagination.SortBy, GetSortValue(lastItem, pagination.SortBy) }
        };

        return cursorHelper.EncodeCursor(new CursorData(values));
    }

    private static object GetSortValue(ProjectWorkspace item, string sortBy)
    {
        return sortBy switch
        {
            "Name" => item.Name,
            "CreatedAt" => item.CreatedAt,
            "UpdatedAt" => item.UpdatedAt,
            _ => item.UpdatedAt
        };
    }
}
