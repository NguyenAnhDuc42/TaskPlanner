using Domain.Enums;
using Domain.Enums.RelationShip;
using Domain.Permission;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Helpers.Permission;

public class PermissionResolver
{
    public AccessLevel ResolveEffectiveAccess(
        PermissionContext context,
        EntityPath path)
    {
        // RULE 1: Check direct explicit access on this resource
        var directAccess = context.GetExplicitAccess(path.EntityLayer, path.EntityId);
        if (directAccess.HasValue)
            return directAccess.Value;

        // RULE 2: If private + no direct access = denied
        if (path.IsPrivate)
            return AccessLevel.None;

        // RULE 3: Branch-based parent checks
        // ProjectTask -> ProjectList -> (ProjectFolder?) -> ProjectSpace
        if (path.EntityLayer == EntityLayerType.ProjectTask)
        {
            if (path.ProjectListId.HasValue)
            {
                var listAccess = context.GetExplicitAccess(EntityLayerType.ProjectList, path.ProjectListId.Value);
                if (listAccess.HasValue) return listAccess.Value;

                if (path.ProjectFolderId.HasValue)
                {
                    var folderAccess = context.GetExplicitAccess(EntityLayerType.ProjectFolder, path.ProjectFolderId.Value);
                    if (folderAccess.HasValue) return folderAccess.Value;
                }

                if (path.ProjectSpaceId.HasValue)
                {
                    var spaceAccess = context.GetExplicitAccess(EntityLayerType.ProjectSpace, path.ProjectSpaceId.Value);
                    if (spaceAccess.HasValue) return spaceAccess.Value;
                }
            }
        }
        // ProjectList -> (ProjectFolder?) -> ProjectSpace
        else if (path.EntityLayer == EntityLayerType.ProjectList)
        {
            if (path.ProjectFolderId.HasValue)
            {
                var folderAccess = context.GetExplicitAccess(EntityLayerType.ProjectFolder, path.ProjectFolderId.Value);
                if (folderAccess.HasValue) return folderAccess.Value;
            }

            if (path.ProjectSpaceId.HasValue)
            {
                var spaceAccess = context.GetExplicitAccess(EntityLayerType.ProjectSpace, path.ProjectSpaceId.Value);
                if (spaceAccess.HasValue) return spaceAccess.Value;
            }
        }
        // ProjectFolder -> ProjectSpace
        else if (path.EntityLayer == EntityLayerType.ProjectFolder)
        {
            if (path.ProjectSpaceId.HasValue)
            {
                var spaceAccess = context.GetExplicitAccess(EntityLayerType.ProjectSpace, path.ProjectSpaceId.Value);
                if (spaceAccess.HasValue) return spaceAccess.Value;
            }
        }
        // ProjectSpace -> no parent
        else if (path.EntityLayer == EntityLayerType.ProjectSpace)
        {
            // Already checked direct access above
        }
        // ChatRoom or others
        else
        {
            var explicitAccess = context.GetExplicitAccess(path.EntityLayer, path.EntityId);
            if (explicitAccess.HasValue) return explicitAccess.Value;
        }

        // RULE 4: Fallback to workspace role default
        return GetRoleDefault(context.Role);
    }

    public bool CanAccess(
        PermissionContext context,
        EntityPath path,
        AccessLevel requiredLevel)
    {
        var effectiveAccess = ResolveEffectiveAccess(context, path);
        
        // AccessLevel hierarchy: Manager (1) > Editor (2) > Viewer (3) > None (0)
        // Wait, checking original definition: Manager=1, Editor=2, Viewer=3. 
        // Guide says: Manager (3) > Editor (2) > Viewer (1) > None (0).
        // Let's check the actual enum again.
        return IsGreaterOrEqual(effectiveAccess, requiredLevel);
    }

    private bool IsGreaterOrEqual(AccessLevel effective, AccessLevel required)
    {
        if (required == AccessLevel.None) return true;
        if (effective == AccessLevel.None) return false;
        
        // Enum: Manager, Editor, Viewer. 
        // Usually Manager is "highest".
        // If values are 1, 2, 3... we need to be careful.
        
        return GetWeight(effective) >= GetWeight(required);
    }

    private static int GetWeight(AccessLevel level) => level switch
    {
        AccessLevel.Manager => 3,
        AccessLevel.Editor => 2,
        AccessLevel.Viewer => 1,
        _ => 0
    };

    private static AccessLevel GetRoleDefault(Role role) => role switch
    {
        Role.Owner => AccessLevel.Manager,
        Role.Admin => AccessLevel.Manager,
        Role.Member => AccessLevel.Editor,
        Role.Guest => AccessLevel.Viewer,
        _ => AccessLevel.None
    };
}