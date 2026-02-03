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

        // Query EntityMembers with User details
        var members = await QueryNoTracking<EntityMember>()
            .Where(em => em.LayerId == request.LayerId && em.LayerType == request.LayerType)
            .ApplyCursor(request.Pagination, CursorHelper)
            .ApplySort(request.Pagination)
            .Take(pageSize + 1)
            .ToListAsync(cancellationToken);

        var hasMore = members.Count > pageSize;
        var displayItems = members.Take(pageSize).ToList();

        // Join with Users to get user details
        var userIds = displayItems.Select(em => em.UserId).ToList();
        var users = await UnitOfWork.Set<User>()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        var dtos = displayItems.Select(em => new EntityMemberDto(
            em.Id,
            em.UserId,
            users.ContainsKey(em.UserId) ? users[em.UserId].Name : "Unknown",
            users.ContainsKey(em.UserId) ? users[em.UserId].Email : "Unknown",
            em.AccessLevel,
            em.CreatedAt
        )).ToList();

        string? nextCursor = null;
        if (hasMore && displayItems.Count > 0)
        {
            var lastItem = displayItems.Last(); // This is EntityMember, has UpdatedAt
            nextCursor = CursorHelper.EncodeCursor(new CursorData(new Dictionary<string, object>
            {
                { "Timestamp", lastItem.UpdatedAt },
                { "Id", lastItem.Id }
            }));
        }

        return new PagedResult<EntityMemberDto>(dtos, nextCursor, hasMore);
    }
}
