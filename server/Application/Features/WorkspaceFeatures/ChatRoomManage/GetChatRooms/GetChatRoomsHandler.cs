using Application.Helper;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services.Permissions;
using Domain;
using Application.Helpers;
using Domain.Entities.Relationship;
using Domain.Entities.Support.Workspace;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.WorkspaceFeatures.ChatRoomManage.GetChatRooms;

public class GetChatRoomsHandler : BaseQueryHandler, IRequestHandler<GetChatRoomsQuery, List<ChatRoomDto>>
{
    public GetChatRoomsHandler(IUnitOfWork unitOfWork, IPermissionService permissionService, ICurrentUserService currentUserService, WorkspaceContext workspaceContext, CursorHelper cursorHelper)
    : base(unitOfWork, permissionService, currentUserService, workspaceContext, cursorHelper) { }

    public async Task<List<ChatRoomDto>> Handle(GetChatRoomsQuery request, CancellationToken cancellationToken)
    {
        // Get chat rooms where user is a member OR room is public in this workspace
        var chatRooms = await UnitOfWork.Set<ChatRoom>()
            .AsNoTracking()
            .Where(cr => cr.ProjectWorkspaceId == request.WorkspaceId && cr.DeletedAt == null)
            .Where(cr => !cr.IsPrivate || cr.Members.Any(m => m.UserId == CurrentUserId && m.DeletedAt == null))
            .Select(cr => new ChatRoomDto(
                cr.Id,
                cr.Name,
                cr.AvatarUrl,
                cr.IsPrivate,
                cr.IsArchived,
                cr.Members.Count(m => m.DeletedAt == null),
                cr.CreatedAt
            ))
            .OrderByDescending(cr => cr.CreatedAt)
            .ToListAsync(cancellationToken);

        return chatRooms;
    }
}
