using Application.Features.WorkspaceFeatures.SelfManagement.GetWorkspaceList;
using Domain.Enums;
using Domain.Enums.RelationShip;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Helpers.Permission;

public class PermissionResolver
{
    private static AccessLevel GetRoleDefault(Role role) => role switch
    {
        Role.Owner => AccessLevel.Manager,
        Role.Admin => AccessLevel.Manager,
        Role.Member => AccessLevel.Editor,
        Role.Guest => AccessLevel.Viewer,
        _ => AccessLevel.None
    };

};