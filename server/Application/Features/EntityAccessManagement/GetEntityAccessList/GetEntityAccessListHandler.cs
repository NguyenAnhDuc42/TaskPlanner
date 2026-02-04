using Application.Common.Results;
using Application.Helper;
using Application.Interfaces.Repositories;
using Domain;
using Application.Helpers;
using Domain.Entities;
using Domain.Entities.Relationship;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.EntityAccessManagement.GetEntityAccessList;

public class GetEntityAccessListHandler : BaseFeatureHandler, IRequestHandler<GetEntityAccessListQuery, List<EntityAccessDto>>
{
    public GetEntityAccessListHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, currentUserService, workspaceContext) { }

    public async Task<List<EntityAccessDto>> Handle(GetEntityAccessListQuery request, CancellationToken cancellationToken)
    {
        // Get parent layer
        var layer = await GetLayer(request.LayerId, request.LayerType);

        // Query EntityAccess records
        var accessRecords = await UnitOfWork.Set<EntityAccess>()
            .AsNoTracking()
            .Where(ea => ea.EntityId == request.LayerId && ea.EntityLayer == request.LayerType)
            .ToListAsync(cancellationToken);

        // Get WorkspaceMember details to get UserIds
        var wmIds = accessRecords.Select(ea => ea.WorkspaceMemberId).Distinct().ToList();
        var workspaceMembers = await UnitOfWork.Set<WorkspaceMember>()
            .AsNoTracking()
            .Where(wm => wmIds.Contains(wm.Id))
            .ToDictionaryAsync(wm => wm.Id, cancellationToken);

        // Get User details
        var userIds = workspaceMembers.Values.Select(wm => wm.UserId).Distinct().ToList();
        var users = await UnitOfWork.Set<User>()
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        var dtos = accessRecords.Select(ea => {
            var wm = workspaceMembers.GetValueOrDefault(ea.WorkspaceMemberId);
            var user = wm != null ? users.GetValueOrDefault(wm.UserId) : null;
            
            return new EntityAccessDto(
                ea.Id,
                user?.Id ?? Guid.Empty,
                user?.Name ?? "Unknown",
                user?.Email ?? "Unknown",
                ea.AccessLevel,
                ea.CreatedAt
            );
        }).ToList();

        return dtos;
    }
}
