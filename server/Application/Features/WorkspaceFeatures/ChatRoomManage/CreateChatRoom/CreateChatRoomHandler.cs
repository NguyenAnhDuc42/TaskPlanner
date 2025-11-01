using System;
using System.ComponentModel.DataAnnotations;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services.Permissions;
using Domain.Entities.Relationship;
using Domain.Entities.Support.Workspace;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.WorkspaceFeatures.ChatRoomManage.CreateChatRoom;

public class CreateChatRoomHandler : BaseCommandHandler, IRequestHandler<CreateChatRoomCommand, Unit>
{
    public CreateChatRoomHandler(IUnitOfWork unitOfWork, IPermissionService permissionService, ICurrentUserService currentUserService)
    : base(unitOfWork, permissionService, currentUserService) { }
    public async Task<Unit> Handle(CreateChatRoomCommand request, CancellationToken cancellationToken)
    {
        await RequirePermissionAsync(request.workspaceId, EntityType.ChatRoom, PermissionAction.Create, cancellationToken);
        var chatRoom = ChatRoom.Create(request.name, request.workspaceId, CurrentUserId, request.inviteMembersInWorkspace, request.avatarUrl);
        if (request.memberIds?.Count > 0)
        {
            var validMemberIds = await UnitOfWork.Set<WorkspaceMember>()
                .Where(wm => wm.ProjectWorkspaceId == request.workspaceId && request.memberIds.Contains(wm.UserId))
                .Select(wm => wm.UserId)
                .ToListAsync(cancellationToken);
            if (validMemberIds.Count != request.memberIds.Count)
                throw new ValidationException("Some members are not part of this workspace.");

            var membersToAdd = ChatRoomMember.AddMembers(chatRoom.Id, validMemberIds);
            chatRoom.AddMembers(membersToAdd);
        }

        await UnitOfWork.Set<ChatRoom>().AddAsync(chatRoom, cancellationToken);
        return Unit.Value;
    }
}
