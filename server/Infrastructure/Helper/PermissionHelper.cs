using System;
using System.Linq;
using Domain.Enums;

public static class PermissionHelper
{
    // Returns only single-bit (atomic) flags declared in the enum
    public static Permission GetAllAtomicPermissions()
    {
        var values = Enum.GetValues(typeof(Permission)).Cast<Permission>()
            .Where(p => p != Permission.None && IsPowerOfTwo((long)p));
        return values.Aggregate(Permission.None, (acc, v) => acc | v);
    }

    private static bool IsPowerOfTwo(long v) => v != 0 && (v & (v - 1)) == 0;

    // Owner permissions = all atomic permissions except Transfer_Ownership (business rule)
    public static Permission GetOwnerPermissions()
    {
        var all = GetAllAtomicPermissions();
        return all & ~Permission.Transfer_Ownership;
    }

    // All defined atomic permissions
    public static Permission GetAllPermissions() => GetAllAtomicPermissions();

    // Convenience: determine if a permission is a "view" type
    public static bool IsViewPermission(Permission permission)
    {
        var viewFlags = Permission.View_Workspace
                      | Permission.View_Spaces
                      | Permission.View_Lists
                      | Permission.View_Tasks
                      | Permission.View_Statuses
                      | Permission.View_Comments
                      | Permission.View_Attachments;
        return (viewFlags & permission) == permission;
    }
}
