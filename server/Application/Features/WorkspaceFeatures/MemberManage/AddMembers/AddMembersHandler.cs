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
    IBackgroundJobService backgroundJob
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

        // 2. PERFORMANCE: Resolve all users in one batch query
        var emails = request.members.Select(m => m.email).ToList();
        var users = await db.Users.Where(u => emails.Contains(u.Email)).ToListAsync(ct);
        var userMap = users.ToDictionary(u => u.Email);

        // 3. LOGIC: Add members using the bulk domain method
        var membersToInvite = request.members
            .Where(m => userMap.ContainsKey(m.email))
            .Select(m => (userMap[m.email].Id, m.role))
            .ToList();

        if (membersToInvite.Any())
        {
            workspace.AddMembers(membersToInvite, context.CurrentMember.Id);
        }

        await db.SaveChangesAsync(ct);

        // 4. Instant Trigger for Member Notifications
        backgroundJob.TriggerOutbox();

        // STAGE: All SignalR notifications and emails are now handled by 
        // WorkspaceMembersAddedBulkEventHandler in the background.

        return Result.Success();
    }
}
