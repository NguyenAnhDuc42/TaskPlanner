using src.Domain.Enums;

namespace src.Helper.Permission;

public enum Permission
{
    CreateTask,
    EditTask,
    DeleteTask,
    ViewTask,
    ManageUsers,
}
public static class CustomClaims
{
    public const string UserId = "userId"; // Represents a GUID now
    public const string WorkspaceId = "workspaceId";
    public const string WorkspaceRole = "workspaceRole";
}

public static class PermissionMappings
{
    private static readonly Dictionary<Role, HashSet<Permission>> _rolePermissions = new()
    {
        [Role.Owner] = new HashSet<Permission>
        {
            Permission.CreateTask, Permission.EditTask, Permission.DeleteTask, Permission.ViewTask,
            Permission.ManageUsers
        },
        [Role.Admin] = new HashSet<Permission>
        {
            Permission.CreateTask, Permission.EditTask, Permission.DeleteTask, Permission.ViewTask,
        },
        [Role.Member] = new HashSet<Permission>
        {
            Permission.CreateTask, Permission.EditTask, Permission.ViewTask,
        },
        [Role.Guest] = new HashSet<Permission>
        {
            Permission.ViewTask
        }
    };

    public static bool HasPermission(Role role, Permission permission)
        => _rolePermissions.TryGetValue(role, out var permissions) && permissions.Contains(permission);
}
