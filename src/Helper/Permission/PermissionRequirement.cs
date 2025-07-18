using System;
using Microsoft.AspNetCore.Authorization;

namespace src.Helper.Permission;

public class PermissionRequirement : IAuthorizationRequirement
{
    public Permission Permission { get; }
    public bool CheckCreator { get; }
    public PermissionRequirement(Permission permission, bool checkCreator = false)
    {
        Permission = permission;
        CheckCreator = checkCreator;
    }

}
