using System;
using Application.Common;
using Domain.Enums;
using Domain.Enums.RelationShip;
using Domain.Enums.Workspace;

namespace Infrastructure.Services.Permissions;

public class PermissionMatrix
{
    private static readonly Dictionary<(EntityType, PermissionAction), PermissionRule> Rules = new()
    {
        #region Workspace & Infrastructure
        [(EntityType.ProjectWorkspace, PermissionAction.Create)] = new()
        {
            Evaluate = ctx => ctx.WorkspaceRole == Role.Owner || ctx.WorkspaceRole == Role.Admin || ctx.WorkspaceRole == Role.Member,
            Description = "Members and above can create spaces"
        },
        [(EntityType.ProjectWorkspace, PermissionAction.Delete)] = new()
        {
            Evaluate = ctx => ctx.IsWorkspaceOwner || ctx.IsCreator,
            Description = "Must be workspace owner or creator"
        },
        [(EntityType.ProjectWorkspace, PermissionAction.ManageSettings)] = new()
        {
            Evaluate = ctx => ctx.IsWorkspaceOwner,
            Description = "Must be workspace owner"
        },
        [(EntityType.WorkspaceMember, PermissionAction.Create)] = new()
        {
            Evaluate = ctx => ctx.WorkspaceRole == Role.Owner || ctx.WorkspaceRole == Role.Admin,
            Description = "Must be workspace owner or admin"
        },
        [(EntityType.WorkspaceMember, PermissionAction.Edit)] = new()
        {
            Evaluate = ctx => ctx.WorkspaceRole == Role.Owner || ctx.WorkspaceRole == Role.Admin,
            Description = "Must be workspace owner or admin"
        },
        [(EntityType.WorkspaceMember, PermissionAction.Delete)] = new()
        {
            Evaluate = ctx => ctx.WorkspaceRole == Role.Owner || ctx.WorkspaceRole == Role.Admin,
            Description = "Must be workspace owner or admin"
        },
        #endregion

        #region Chat & Communications
        [(EntityType.ChatRoom, PermissionAction.Create)] = new()
        {
            Evaluate = ctx => ctx.WorkspaceRole == Role.Owner || ctx.WorkspaceRole == Role.Admin || ctx.WorkspaceRole == Role.Member,
            Description = "Members and above can create chat rooms"
        },
        [(EntityType.ChatRoom, PermissionAction.Delete)] = new()
        {
            Evaluate = ctx =>
                ctx.IsWorkspaceOwner ||                                  
                ctx.IsCreator ||                                         
                ctx.ChatRoomRole == ChatRoomRole.Owner,                  
            Description = "Must be workspace owner, chat room owner, or creator"
        },
        [(EntityType.ChatRoom, PermissionAction.Edit)] = new()
        {
            Evaluate = ctx =>
                ctx.IsWorkspaceOwner ||
                ctx.ChatRoomRole == ChatRoomRole.Owner,
            Description = "Must be workspace owner or chat room owner"
        },
        [(EntityType.ChatRoom, PermissionAction.ManageSettings)] = new()
        {
            Evaluate = ctx =>
                ctx.IsWorkspaceOwner ||
                ctx.ChatRoomRole == ChatRoomRole.Owner,
            Description = "Must be workspace owner or chat room owner"
        },
        [(EntityType.ChatRoomMember, PermissionAction.Delete)] = new()
        {
            Evaluate = ctx =>
                ctx.IsWorkspaceOwner ||
                ctx.ChatRoomRole == ChatRoomRole.Owner,
            Description = "Must be workspace owner or chat room owner"
        },
        [(EntityType.ChatRoomMember, PermissionAction.Edit)] = new()
        {
            Evaluate = ctx =>
                (ctx.IsWorkspaceOwner || ctx.IsChatRoomOwner) &&
                !ctx.IsUserBannedFromChatRoom,  // Cannot manage if you're banned
            Description = "Only room owner can manage members, and must not be banned"
        },
        [(EntityType.ChatRoomMember, PermissionAction.Create)] = new()
        {
            Evaluate = ctx =>
                (ctx.IsWorkspaceOwner || ctx.IsChatRoomOwner) &&
                !ctx.IsEntityPrivate,  // Cannot invite to private rooms without explicit permission
            Description = "Only owner can invite, only to public/non-restricted rooms"
        },
        [(EntityType.ChatMessage, PermissionAction.Edit)] = new()
        {
            Evaluate = ctx =>
                ctx.IsCreator &&
                !ctx.IsUserBannedFromChatRoom,
            Description = "Only message sender can edit within 24h, and must not be banned"
        },
        [(EntityType.ChatMessage, PermissionAction.Delete)] = new()
        {
            Evaluate = ctx =>
                ctx.IsCreator ||  // Creator within 24h
                ctx.IsWorkspaceOwner ||
                ctx.IsChatRoomOwner,
            Description = "Creator (within 24h), workspace owner, or room owner can delete"
        },
        [(EntityType.ChatMessage, PermissionAction.Comment)] = new()
        {
            Evaluate = ctx =>
                !ctx.IsUserBannedFromChatRoom &&
                !ctx.IsUserMutedInChatRoom,
            Description = "Cannot comment if banned or muted"
        },
        #endregion

        #region Tasks & Project Hierarchy
        [(EntityType.ProjectSpace, PermissionAction.Create)] = new()
        {
            Evaluate = ctx =>
                ctx.IsWorkspaceOwner ||
                ctx.IsEntityManager ||
                ctx.EntityAccess == AccessLevel.Editor,
            Description = "Must have editor access or above on the space to create folders/lists"
        },
        [(EntityType.ProjectList, PermissionAction.Create)] = new()
        {
            Evaluate = ctx =>
                ctx.IsWorkspaceOwner ||
                ctx.IsEntityManager ||
                ctx.EntityAccess == AccessLevel.Editor,
            Description = "Must have editor access or above on the list to create tasks"
        },
        [(EntityType.ProjectTask, PermissionAction.Create)] = new()
        {
            Evaluate = ctx => 
                !ctx.IsEntityArchived &&
                (ctx.IsWorkspaceOwner || ctx.IsEntityManager || ctx.EntityAccess == AccessLevel.Editor),
            Description = "Must have editor access or above to create tasks"
        },
        [(EntityType.ProjectTask, PermissionAction.Edit)] = new()
        {
            Evaluate = ctx =>
                ctx.IsWorkspaceOwner ||
                ctx.IsEntityManager ||
                (ctx.EntityAccess == AccessLevel.Editor && ctx.IsCreator),
            Description = "Must be workspace owner, manager access, or editor who created it"
        },
        [(EntityType.ProjectTask, PermissionAction.Delete)] = new()
        {
            Evaluate = ctx =>
                ctx.IsWorkspaceOwner ||
                ctx.IsEntityManager ||
                (ctx.EntityAccess == AccessLevel.Editor && ctx.IsCreator),
            Description = "Must be workspace owner, manager access, or editor who created it"
        },
        [(EntityType.ProjectTask, PermissionAction.Assign)] = new()
        {
            Evaluate = ctx =>
                ctx.IsWorkspaceOwner ||
                ctx.IsEntityManager ||
                ctx.EntityAccess == AccessLevel.Editor,
            Description = "Must have editor access or above"
        },
        [(EntityType.ProjectTask, PermissionAction.ChangeStatus)] = new()
        {
            Evaluate = ctx =>
                !ctx.IsEntityArchived &&  // Cannot change status if archived
                (ctx.IsWorkspaceOwner || ctx.IsEntityManager || ctx.EntityAccess == AccessLevel.Editor),
            Description = "Cannot change status of archived tasks"
        },
        [(EntityType.ProjectList, PermissionAction.Archive)] = new()
        {
            Evaluate = ctx =>
                (ctx.IsWorkspaceOwner || ctx.IsEntityManager) &&
                !ctx.IsEntityArchived,  // Must have no active tasks
            Description = "Can only archive empty lists, must have manager access"
        },
        [(EntityType.ProjectList, PermissionAction.Edit)] = new()
        {
            Evaluate = ctx =>
                !ctx.IsEntityArchived &&  // Cannot edit if archived
                (ctx.IsWorkspaceOwner || ctx.IsEntityManager || (ctx.EntityAccess == AccessLevel.Editor && ctx.IsCreator)),
            Description = "Cannot edit archived lists"
        },
        [(EntityType.ProjectFolder, PermissionAction.Archive)] = new()
        {
            Evaluate = ctx =>
                (ctx.IsWorkspaceOwner || ctx.IsEntityManager) &&
                !ctx.IsEntityArchived,  // Must have no active lists/folders
            Description = "Can only archive empty folders, must have manager access"
        },
        #endregion

        #region Custom Statuses (Workflows)
        [(EntityType.Status, PermissionAction.Create)] = new()
        {
            DataNeeds = PermissionDataNeeds.WorkspaceRole | PermissionDataNeeds.EntityAccess,
            Evaluate = ctx => ctx.WorkspaceRole == Role.Owner || ctx.WorkspaceRole == Role.Admin || ctx.IsEntityManager,
            Description = "Admins and managers can create custom statuses for their layers"
        },
        [(EntityType.Status, PermissionAction.Edit)] = new()
        {
            DataNeeds = PermissionDataNeeds.WorkspaceRole | PermissionDataNeeds.EntityAccess,
            Evaluate = ctx => ctx.WorkspaceRole == Role.Owner || ctx.WorkspaceRole == Role.Admin || ctx.IsEntityManager,
            Description = "Admins and managers can edit statuses for their layers"
        },
        [(EntityType.Status, PermissionAction.Delete)] = new()
        {
            DataNeeds = PermissionDataNeeds.WorkspaceRole | PermissionDataNeeds.EntityAccess,
            Evaluate = ctx => ctx.WorkspaceRole == Role.Owner || ctx.WorkspaceRole == Role.Admin || ctx.IsEntityManager,
            Description = "Admins and managers can delete custom statuses"
        },
        #endregion
    };

    public static bool CanPerform(EntityType entityType, PermissionAction action, PermissionContext context)
    {
        if (context.IsPrivacyBlocked)
        {
            return false;
        }

        if (!Rules.TryGetValue((entityType, action), out var rule))
        {
            // Default deny if rule not found
            return false;
        }

        try
        {
            return rule.Evaluate(context);
        }
        catch
        {
            // Log rule evaluation errors
            return false;
        }
    }

    public static PermissionRule? GetRule(EntityType entityType, PermissionAction action)
    {
        Rules.TryGetValue((entityType, action), out var rule);
        return rule;
    }

    public static string? GetRuleDescription(EntityType entityType, PermissionAction action)
    {
        return Rules.TryGetValue((entityType, action), out var rule) ? rule.Description : null;
    }
}
