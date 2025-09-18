using System;
using System.Linq;
using Domain.Enums;

public static class PermissionHelper
{
    public static Permission MapAccessLevelToPermissionMask(EntityType entityType, AccessLevel level)
    {
        return entityType switch
        {
            EntityType.ProjectSpace => level switch
            {
                AccessLevel.Manager => Permission.View_Spaces
                                      | Permission.Edit_Spaces
                                      | Permission.Delete_Spaces
                                      | Permission.Archive_Spaces
                                      // actions inside the space
                                      | Permission.Create_Lists
                                      | Permission.Edit_Lists
                                      | Permission.Delete_Lists
                                      | Permission.Create_Tasks
                                      | Permission.Edit_Tasks
                                      | Permission.Delete_Tasks,
                AccessLevel.Editor => Permission.View_Spaces
                                      | Permission.Edit_Spaces
                                      | Permission.Create_Lists
                                      | Permission.Create_Tasks
                                      | Permission.Edit_Tasks,
                AccessLevel.Viewer => Permission.View_Spaces,
                _ => Permission.None
            },

            EntityType.ProjectFolder => level switch
            {
                AccessLevel.Manager => Permission.View_Lists
                                      | Permission.Create_Lists
                                      | Permission.Edit_Lists
                                      | Permission.Delete_Lists
                                      | Permission.Reorder_Lists
                                      | Permission.Create_Tasks
                                      | Permission.Edit_Tasks
                                      | Permission.Delete_Tasks,
                AccessLevel.Editor => Permission.View_Lists
                                      | Permission.Create_Lists
                                      | Permission.Create_Tasks
                                      | Permission.Edit_Tasks,
                AccessLevel.Viewer => Permission.View_Lists,
                _ => Permission.None
            },

            EntityType.ProjectList => level switch
            {
                AccessLevel.Manager => Permission.View_Lists | Permission.Create_Tasks | Permission.Edit_Lists | Permission.Delete_Lists
                                      | Permission.Reorder_Lists | Permission.Edit_Tasks | Permission.Delete_Tasks | Permission.Assign_Tasks
                                      | Permission.Change_Task_Status,
                AccessLevel.Editor => Permission.View_Lists | Permission.Create_Tasks | Permission.Edit_Tasks,
                AccessLevel.Viewer => Permission.View_Lists | Permission.View_Tasks,
                _ => Permission.None
            },

            EntityType.ProjectTask => level switch
            {
                AccessLevel.Manager => Permission.View_Tasks | Permission.Create_Tasks | Permission.Edit_Tasks
                                      | Permission.Delete_Tasks | Permission.Assign_Tasks | Permission.Change_Task_Status
                                      | Permission.View_Comments | Permission.Create_Comments,
                AccessLevel.Editor => Permission.View_Tasks | Permission.Create_Tasks | Permission.Edit_Tasks | Permission.Create_Comments,
                AccessLevel.Viewer => Permission.View_Tasks | Permission.View_Comments,
                _ => Permission.None
            },

            _ => Permission.None
        };
    }

    // Map a workspace Role to workspace-level permission mask.
    // These are *global workspace permissions* (create spaces, manage members, settings, etc.).
    public static Permission MapRoleToPermissionMask(Role role)
    {
        return role switch
        {
            Role.Owner => Permission.All,
            Role.Admin => Permission.Workspace_Admin | Permission.Member_Admin | Permission.Content_Admin,
            Role.Member => Permission.View_Spaces
                           | Permission.Create_Lists
                           | Permission.Create_Tasks
                           | Permission.View_Lists
                           | Permission.View_Tasks
                           | Permission.Create_Comments
                           | Permission.View_Comments,
            Role.Guest => Permission.View_Workspace | Permission.View_Spaces | Permission.View_Lists | Permission.View_Tasks,
            _ => Permission.None
        };
    }

    // small utility for checking "does mask include required permission?"
    public static bool MaskHas(Permission mask, Permission required) => (mask & required) == required;
}

