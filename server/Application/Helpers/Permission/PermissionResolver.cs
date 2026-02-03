using Domain.Enums;
using Domain.Enums.RelationShip;
using Domain.Permission;

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

        // RULE 2: If this resource is private + no direct access = denied
        if (path.IsPrivate)
            return AccessLevel.None;

        // RULE 3: Walk up hierarchy from direct parent
        if (path.DirectParentId.HasValue && path.DirectParentType.HasValue)
        {
            var parentAccess = context.GetExplicitAccess(
                path.DirectParentType.Value,
                path.DirectParentId.Value);

            if (parentAccess.HasValue)
                return parentAccess.Value;

            // If parent is private, STOP - don't inherit past it
            if (path.IsDirectParentPrivate == true)
                return GetWorkspaceRoleDefault(context.WorkspaceRole);
        }

        // ProjectTask -> ProjectList -> (ProjectFolder?) -> ProjectSpace
        // If we are at Task, we already checked List above as direct parent.
        // If List was public and had no access, we check Folder or Space.
        
        // If parent was List (at Task level) and not private, check Folder
        if (path.EntityLayer == EntityLayerType.ProjectTask && 
            path.DirectParentType == EntityLayerType.ProjectList &&
            path.IsDirectParentPrivate != true &&
            path.ProjectFolderId.HasValue)
        {
            var folderAccess = context.GetExplicitAccess(EntityLayerType.ProjectFolder, path.ProjectFolderId.Value);
            if (folderAccess.HasValue) return folderAccess.Value;
            
            if (path.IsFolderPrivate == true)
                return GetWorkspaceRoleDefault(context.WorkspaceRole);
        }

        // Check Space (if we haven't stopped at Task->List or Task->List->Folder)
        if (path.ProjectSpaceId.HasValue)
        {
            bool canCheckSpace = false;
            if (path.EntityLayer == EntityLayerType.ProjectTask)
            {
                // Task -> List(public) -> [Folder(public)?] -> Space
                bool folderOk = !path.ProjectFolderId.HasValue || path.IsFolderPrivate != true;
                if (path.IsDirectParentPrivate != true && folderOk)
                    canCheckSpace = true;
            }
            else if (path.EntityLayer == EntityLayerType.ProjectList)
            {
                // List -> Folder(public) -> Space
                if (path.DirectParentType == EntityLayerType.ProjectFolder && path.IsDirectParentPrivate != true)
                    canCheckSpace = true;
            }

            if (canCheckSpace)
            {
                var spaceAccess = context.GetExplicitAccess(EntityLayerType.ProjectSpace, path.ProjectSpaceId.Value);
                if (spaceAccess.HasValue) return spaceAccess.Value;
            }
        }

        // RULE 4: Fallback to workspace role default
        return GetWorkspaceRoleDefault(context.WorkspaceRole);
    }

    public bool CanAccess(
        PermissionContext context,
        EntityPath path,
        AccessLevel requiredLevel)
    {
        var effectiveAccess = ResolveEffectiveAccess(context, path);
        return IsGreaterOrEqual(effectiveAccess, requiredLevel);
    }

    private bool IsGreaterOrEqual(AccessLevel effective, AccessLevel required)
    {
        if (required == AccessLevel.None) return true;
        if (effective == AccessLevel.None) return false;
        return GetWeight(effective) >= GetWeight(required);
    }

    private static int GetWeight(AccessLevel level) => level switch
    {
        AccessLevel.Manager => 3,
        AccessLevel.Editor => 2,
        AccessLevel.Viewer => 1,
        _ => 0
    };

    private AccessLevel GetWorkspaceRoleDefault(Role role) => role switch
    {
        Role.Owner => AccessLevel.Manager,
        Role.Admin => AccessLevel.Manager,
        Role.Member => AccessLevel.Editor,
        Role.Guest => AccessLevel.Viewer,
        _ => AccessLevel.None
    };
}