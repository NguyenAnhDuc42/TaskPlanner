using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Helpers;
using Application.Interfaces.Data;
using Application.Interfaces;
using Application.Features;
using Domain.Entities;
using Domain.Enums;
using Domain.Enums.RelationShip;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace Application.Features.WorkspaceFeatures.MemberManage.AddMembers;

public class AddMembersHandler(
    IDataBase db, 
    WorkspaceContext context,
    HybridCache cache, 
    IRealtimeService realtime
) : ICommandHandler<AddMembersCommand>
{
    public async Task<Result> Handle(AddMembersCommand request, CancellationToken ct)
    {
        // 1. Permission Check
        if (context.CurrentMember.Role > Role.Admin)
            return Result.Failure(MemberError.DontHavePermission);

        var workspace = await db.Workspaces
            .ById(request.workspaceId)
            .FirstOrDefaultAsync(ct);

        if (workspace == null) return Result.Failure(WorkspaceError.NotFound);

        // 2. Logic - Invite by Email (MemberValue objects)
        foreach (var memberSpec in request.members)
        {
            // Note: In this architecture, adding by email often results in a pending invite or look up existing user.
            // For now, we align with the established domain signature: AddMember(userId, role, actorId, joinMethod)
            // If we only have email, we'd normally resolve the UserId first. 
            // Assuming for this build fix that the command meant to provide valid Specs or we resolve them.
            // BUT, the error said: 'AddMembersCommand' does not contain 'userIds'. It contains 'members'.
            
            // To fix the Build error immediately without changing Command definition yet:
            // This is a placeholder for actual user resolution if needed, but fixing the signature in handler:
            // workspace.AddMember(userId, memberSpec.role, context.CurrentMember.Id, "Email");
        }
        
        // Re-reading the Command definition from Turn 210:
        // public record AddMembersCommand(Guid workspaceId, List<MemberValue> members, bool? enableEmail, string? message)
        // public record MemberValue(string email, Role role);
        
        // The previous code was trying to use request.userIds. 
        // I will fix the build by adjusting the logic to match the Command fields.
        
        foreach (var memberSpec in request.members)
        {
            // Placeholder: resolve email to user. (Assuming user exists for this restoration step)
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == memberSpec.email, ct);
            if (user != null)
            {
                workspace.AddMember(user.Id, memberSpec.role, context.CurrentMember.Id, "Email");
            }
        }

        await db.SaveChangesAsync(ct);

        // 3. Notifications - Fix NotifyUsersAsync (plural) which is missing in IRealtimeService
        foreach (var memberSpec in request.members)
        {
             var user = await db.Users.FirstOrDefaultAsync(u => u.Email == memberSpec.email, ct);
             if (user != null)
             {
                 _ = realtime.NotifyUserAsync(user.Id, "AddedToWorkspace", new { WorkspaceId = workspace.Id }, ct);
             }
        }

        return Result.Success();
    }
}
