using System;
using System.Collections.Generic;
using System.Text;
using Domain.Enums;

namespace Application.Features.WorkspaceFeatures.MemberManage.GetMembers;

public class MemberRow
{
    public Guid Id { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string AvatarUrl { get; set; } = null!;
    public Role Role { get; set; }
}
public static class GetMembersSQL
{
    public const string Asc = @"
    SELECT 
        u.id,
        u.updated_at AS UpdatedAt,
        u.full_name AS Name,
        u.email AS Email,
        u.avatar_url AS AvatarUrl,
        wm.role AS Role
    FROM 
        users u
    JOIN workspace_members wm 
        ON wm.user_id = u.id AND wm.project_workspace_id = @WorkspaceId
    WHERE 
        (@name IS NULL OR u.full_name ILIKE '%' || @name || '%') AND 
        (@email IS NULL OR u.email ILIKE '%' || @email || '%') AND 
        (@role IS NULL OR wm.role = @role)
        (
            @cursorTimestamp IS NULL OR
                (
                    u.updated_at > @cursorTimestamp OR 
                    (u.updated_at = @cursorTimestamp AND u.id > @cursorId)
                )
        )
    ORDER BY
        u.updated_at ASC, u.id ASC
    LIMIT @PageSizePLusOne;

    ";
    public const string Desc = @"
    SELECT 
        u.id,
        u.updated_at AS UpdatedAt,
        u.full_name AS Name,
        u.email AS Email,
        u.avatar_url AS AvatarUrl,
        wm.role AS Role
    FROM 
        users u
    JOIN workspace_members wm 
        ON wm.user_id = u.id AND wm.project_workspace_id = @WorkspaceId
    WHERE 
        (@name IS NULL OR u.full_name ILIKE '%' || @name || '%') AND 
        (@email IS NULL OR u.email ILIKE '%' || @email || '%') AND 
        (@role IS NULL OR wm.role = @role)
        (
            @cursorTimestamp IS NULL OR
                (
                    u.updated_at < @cursorTimestamp OR 
                    (u.updated_at = @cursorTimestamp AND u.id < @cursorId)
                )
        )
    ORDER BY
        u.updated_at DESC, u.id DESC
    LIMIT @PageSizePLusOne;

    ";
}

