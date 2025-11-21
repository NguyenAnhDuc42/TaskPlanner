using Application.Common.Filters;
using Application.Helper;
using Domain.Entities.Relationship;

namespace Application.Features.EntityMemberManagement.EntityMemberList;

public static class EntityMemberCursorPaginationExtensions
{
    public static IQueryable<EntityMember> ApplyCursor(this IQueryable<EntityMember> query, CursorPaginationRequest pagination, CursorHelper cursorHelper)
    {
        if (string.IsNullOrWhiteSpace(pagination.Cursor))
            return query;

        var cursorData = cursorHelper.DecodeCursor(pagination.Cursor);
        if (cursorData?.Values == null || !cursorData.Values.ContainsKey("Timestamp") || !cursorData.Values.ContainsKey("Id"))
            return query;

        if (!DateTimeOffset.TryParse(cursorData.Values["Timestamp"].ToString(), out var timestamp))
            return query;

        if (!Guid.TryParse(cursorData.Values["Id"].ToString(), out var id))
            return query;

        return pagination.Direction == SortDirection.Ascending
            ? query.Where(em => em.UpdatedAt > timestamp || (em.UpdatedAt == timestamp && em.Id.CompareTo(id) > 0))
            : query.Where(em => em.UpdatedAt < timestamp || (em.UpdatedAt == timestamp && em.Id.CompareTo(id) < 0));
    }

    public static IQueryable<EntityMember> ApplySort(this IQueryable<EntityMember> query, CursorPaginationRequest pagination)
    {
        return pagination.Direction == SortDirection.Ascending
            ? query.OrderBy(em => em.UpdatedAt).ThenBy(em => em.Id)
            : query.OrderByDescending(em => em.UpdatedAt).ThenByDescending(em => em.Id);
    }
}
