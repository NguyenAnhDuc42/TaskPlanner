using Application.Helpers;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Domain.Entities.Relationship;
using Domain.Enums.RelationShip;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.EntityAccessManagement.GetEntityAccessList;

public class GetEntityAccessListHandler : BaseFeatureHandler, IRequestHandler<GetEntityAccessListQuery, List<EntityAccessMemberDto>>
{
    public GetEntityAccessListHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, currentUserService, workspaceContext) { }

    public async Task<List<EntityAccessMemberDto>> Handle(GetEntityAccessListQuery request, CancellationToken cancellationToken)
    {
        var layer = await GetLayer(request.LayerId, request.LayerType);
        var creatorId = layer.CreatorId;

        var accessRecords = await UnitOfWork.Set<EntityAccess>()
            .AsNoTracking()
            .Where(ea =>
                ea.DeletedAt == null &&
                ea.EntityId == request.LayerId &&
                ea.EntityLayer == request.LayerType)
            .ToListAsync(cancellationToken);

        if (accessRecords.Count == 0) return [];

        var memberIds = accessRecords.Select(ea => ea.WorkspaceMemberId).Distinct().ToList();
        var members = await UnitOfWork.Set<WorkspaceMember>()
            .AsNoTracking()
            .Where(wm => memberIds.Contains(wm.Id) && wm.ProjectWorkspaceId == WorkspaceId && wm.DeletedAt == null)
            .Select(wm => new { wm.Id, wm.UserId })
            .ToDictionaryAsync(wm => wm.Id, cancellationToken);

        var userIds = members.Values.Select(m => m.UserId).Distinct().ToList();
        var users = await UnitOfWork.Set<User>()
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id) && u.DeletedAt == null)
            .Select(u => new { u.Id, u.Name, u.Email })
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        var result = new List<EntityAccessMemberDto>(accessRecords.Count);

        foreach (var access in accessRecords)
        {
            if (!members.TryGetValue(access.WorkspaceMemberId, out var member) || !users.TryGetValue(member.UserId, out var user))
            {
                continue;
            }

            result.Add(new EntityAccessMemberDto(
                access.WorkspaceMemberId,
                user.Id,
                user.Name,
                user.Email,
                access.AccessLevel,
                access.CreatedAt,
                creatorId.HasValue && creatorId.Value == user.Id
            ));
        }

        return result;
    }
}
