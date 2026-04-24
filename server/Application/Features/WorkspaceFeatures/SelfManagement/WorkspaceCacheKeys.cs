using System;
using System.Text;

namespace Application.Features.WorkspaceFeatures;

public static class WorkspaceCacheKeys
{
    public static string WorkspaceListTag(Guid userId) => $"user:{userId}:workspaces";

    public static string WorkspaceList(Guid userId, GetWorksapceListQuery query)
    {
        var sb = new StringBuilder();
        sb.Append($"{WorkspaceListTag(userId)}:list");
        sb.Append($"?cursor={query.Pagination.Cursor}");
        sb.Append($"&size={query.Pagination.PageSize}");
        sb.Append($"&sort={query.Pagination.SortBy}");
        sb.Append($"&dir={query.Pagination.Direction}");
        
        if (!string.IsNullOrEmpty(query.filter.Name))
        {
            sb.Append($"&name={query.filter.Name}");
        }
        if (query.filter.Owned.HasValue && query.filter.Owned.Value)
        {
            sb.Append("&owned=true");
        }
        if (query.filter.isArchived.HasValue && query.filter.isArchived.Value)
        {
            sb.Append("&archived=true");
        }

        return sb.ToString();
    }

    public static string MemberList(Guid workspaceId, GetMembersQuery query)
    {
        var sb = new StringBuilder();
        sb.Append($"workspaces:{workspaceId}:members:list");
        sb.Append($"?cursor={query.pagination.Cursor}");
        sb.Append($"&size={query.pagination.PageSize}");
        sb.Append($"&sort={query.pagination.SortBy}");
        sb.Append($"&dir={query.pagination.Direction}");
        
        if (!string.IsNullOrEmpty(query.filter.Name))
        {
            sb.Append($"&name={query.filter.Name}");
        }
        if (query.filter.Email != null)
        {
            sb.Append($"&email={query.filter.Email}");
        }
        if (query.filter.Role != null)
        {
            sb.Append($"&role={query.filter.Role}");
        }
        return sb.ToString();
    }
}
