using System;
using System.Collections.Generic;
using System.Text;
using Domain.Enums;

namespace Application.Features.WorkspaceFeatures.MemberManage.GetMembers;

public class MemberRow
{
    public Guid Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? JoinedAt { get; set; }
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public Role Role { get; set; }
}
public static class GetMembersSQL
{
    public const string Asc = @"
    SELECT 
        u.id,
        wm.created_at AS CreatedAt,
        wm.joined_at AS JoinedAt,
        u.name ,
        u.email AS Email,
        wm.role AS Role
    FROM 
        users u
    JOIN workspace_members wm 
        ON wm.user_id = u.id AND wm.project_workspace_id = @WorkspaceId
    WHERE 
        wm.deleted_at IS NULL AND
        (@name IS NULL OR u.name ILIKE '%' || @name || '%') AND 
        (@email IS NULL OR u.email ILIKE '%' || @email || '%') AND 
        (@role IS NULL OR wm.role = @role) AND
        (
            @cursorTimestamp IS NULL OR
                (
                    wm.created_at > @cursorTimestamp OR 
                    (wm.created_at = @cursorTimestamp AND u.id > @cursorId)
                )
        )
    ORDER BY
        wm.created_at ASC, u.id ASC
    LIMIT @PageSizePLusOne;

    ";
    public const string Desc = @"
    SELECT 
        u.id,
        wm.created_at AS CreatedAt,
        wm.joined_at AS JoinedAt,
        u.name,
        u.email AS Email,
        wm.role AS Role
    FROM 
        users u
    JOIN workspace_members wm 
        ON wm.user_id = u.id AND wm.project_workspace_id = @WorkspaceId
    WHERE 
        wm.deleted_at IS NULL AND
        (@name IS NULL OR u.name ILIKE '%' || @name || '%') AND 
        (@email IS NULL OR u.email ILIKE '%' || @email || '%') AND 
        (@role IS NULL OR wm.role = @role) AND
        (
            @cursorTimestamp IS NULL OR
                (
                    wm.created_at < @cursorTimestamp OR 
                    (wm.created_at = @cursorTimestamp AND u.id < @cursorId)
                )
        )
    ORDER BY
        wm.created_at DESC, u.id DESC
    LIMIT @PageSizePLusOne;

    ";
}

