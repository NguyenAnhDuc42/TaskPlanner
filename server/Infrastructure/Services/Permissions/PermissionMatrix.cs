using System;
using Domain.Enums;
using Domain.Enums.RelationShip;

namespace Infrastructure.Services.Permissions;

public class PermissionMatrix
{
    private static readonly Dictionary<AccessLevel, PermissionAction[]> AccessMap = new()
    {
        [AccessLevel.Manager] = [PermissionAction.View, PermissionAction.Create, PermissionAction.Edit, PermissionAction.Delete, PermissionAction.Comment, PermissionAction.UploadAttachment],
        [AccessLevel.Editor] = [PermissionAction.View, PermissionAction.Create, PermissionAction.Edit, PermissionAction.Comment, PermissionAction.UploadAttachment], // delete handled with Creator check
        [AccessLevel.Viewer] = [PermissionAction.View]
    };

    private static readonly Dictionary<Role, PermissionAction[]> WorkspaceMap = new()
    {
        [Role.Owner] = Enum.GetValues<PermissionAction>(), 
        [Role.Admin] = [PermissionAction.View, PermissionAction.Create, PermissionAction.Edit, PermissionAction.Delete, PermissionAction.Comment, PermissionAction.UploadAttachment, PermissionAction.ManageSettings, PermissionAction.InviteMember, PermissionAction.RemoveMember, PermissionAction.ManageRoles],
        [Role.Member] = [PermissionAction.View, PermissionAction.Create, PermissionAction.Edit, PermissionAction.Comment, PermissionAction.UploadAttachment],
        [Role.Guest] = [PermissionAction.View]
    };

    public static bool Can(AccessLevel level, PermissionAction PermissionAction, bool isCreator = false)
    {
        if (level == AccessLevel.Manager) return true;
        if (!AccessMap.TryGetValue(level, out var allowed)) return false;
        if (PermissionAction == PermissionAction.Delete && level == AccessLevel.Editor) return isCreator; 
        return allowed.Contains(PermissionAction);
    }

    public static bool Can(Role role, PermissionAction permissionAction)
    {
        if (!WorkspaceMap.TryGetValue(role, out var allowed)) return false;
        return allowed.Contains(permissionAction);
    }
}
