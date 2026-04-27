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
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace Application.Features.WorkspaceFeatures;

public class AddMembersHandler(IDataBase db, WorkspaceContext context) : ICommandHandler<AddMembersCommand>
{
    public async Task<Result> Handle(AddMembersCommand request, CancellationToken ct)
    {
        if (context.CurrentMember.Role > Role.Admin)
            return Result.Failure(MemberError.DontHavePermission);

        var workspace = await db.Workspaces
            .ById(request.workspaceId)
            .FirstOrDefaultAsync(ct);

        if (workspace == null) return Result.Failure(WorkspaceError.NotFound);

        var members = request.members;
        if (!members.Any()) return Result.Success();

        await db.Connection.ExecuteAsync(AddMembersSQL.BulkAddMembers, new
        {
            WorkspaceId = workspace.Id,
            CreatorId = context.CurrentMember.Id,
            Emails = members.Select(m => m.email).ToArray(),
            Roles = members.Select(m => m.role.ToString()).ToArray(),
            Theme = Domain.Enums.Theme.Dark.ToString()
        });


        return Result.Success();
    }
}
