using System;
using System.Text;
using Application.Features.WorkspaceFeatures.SelfManagement.GetWorkspaceList;

namespace Application.Features.WorkspaceFeatures.SelfManagement;

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
}
