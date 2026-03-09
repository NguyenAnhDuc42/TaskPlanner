using System;
using System.Text;
using Application.Features.WorkspaceFeatures.MemberManage.GetMembers;

namespace Application.Features.WorkspaceFeatures.MemberManage;

public static class WorkspaceCacheKeys
{
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
