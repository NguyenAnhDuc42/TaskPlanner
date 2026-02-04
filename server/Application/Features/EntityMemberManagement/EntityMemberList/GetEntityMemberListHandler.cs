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

namespace Application.Features.EntityMemberManagement.EntityMemberList;

public class GetEntityMemberListHandler : BaseQueryHandler, IRequestHandler<GetEntityMemberListQuery, PagedResult<EntityMemberDto>>
{
    public GetEntityMemberListHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext, CursorHelper cursorHelper)
        : base(unitOfWork, currentUserService, workspaceContext, cursorHelper) { }

    public async Task<PagedResult<EntityMemberDto>> Handle(GetEntityMemberListQuery request, CancellationToken cancellationToken)
    {
        // Get parent layer
        var layer = await GetLayer(request.LayerId, request.LayerType);

        var pageSize = request.Pagination.PageSize;

        // Query EntityAccess records
        var accessRecords = await QueryNoTracking<EntityAccess>()
            .Where(ea => ea.EntityId == request.LayerId && ea.EntityLayer == request.LayerType)
            .ApplyCursor(request.Pagination, CursorHelper)
            .ApplySort(request.Pagination)
            .Take(pageSize + 1)
            .ToListAsync(cancellationToken);

        var hasMore = accessRecords.Count > pageSize;
        var displayItems = accessRecords.Take(pageSize).ToList();

        // Get WorkspaceMember details to get UserIds
        var wmIds = displayItems.Select(ea => ea.WorkspaceMemberId).ToList();
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

        var dtos = displayItems.Select(ea => {
            var wm = workspaceMembers.GetValueOrDefault(ea.WorkspaceMemberId);
            var user = wm != null ? users.GetValueOrDefault(wm.UserId) : null;
            
            return new EntityMemberDto(
                ea.Id,
                user?.Id ?? Guid.Empty,
                user?.Name ?? "Unknown",
                user?.Email ?? "Unknown",
                ea.AccessLevel,
                ea.CreatedAt
            );
        }).ToList();

        string? nextCursor = null;
        if (hasMore && displayItems.Count > 0)
        {
            var lastItem = displayItems.Last();
            nextCursor = CursorHelper.EncodeCursor(new CursorData(new Dictionary<string, object>
            {
                { "Timestamp", lastItem.UpdatedAt },
                { "Id", lastItem.Id }
            }));
        }

        return new PagedResult<EntityMemberDto>(dtos, nextCursor, hasMore);
    }
}
