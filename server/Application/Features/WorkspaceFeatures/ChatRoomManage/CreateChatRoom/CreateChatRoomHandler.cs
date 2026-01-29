using System;
using System.ComponentModel.DataAnnotations;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services.Permissions;
using Domain;
using Application.Helpers;
using Domain.Entities.ProjectEntities;
using Domain.Entities.Relationship;
using Domain.Entities.Support.Workspace;
using Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.WorkspaceFeatures.ChatRoomManage.CreateChatRoom;

public class CreateChatRoomHandler : BaseCommandHandler, IRequestHandler<CreateChatRoomCommand, Unit>
{
    public CreateChatRoomHandler(IUnitOfWork unitOfWork, IPermissionService permissionService, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
    : base(unitOfWork, permissionService, currentUserService, workspaceContext) { }
    public async Task<Unit> Handle(CreateChatRoomCommand request, CancellationToken cancellationToken)
    {
        var workspace = await UnitOfWork.Set<ProjectWorkspace>()
            .FirstOrDefaultAsync(wp => wp.Id == request.workspaceId, cancellationToken);
        if (workspace == null) throw new ValidationException("Workspace does not exist.");
        cancellationToken.ThrowIfCancellationRequested();

        await RequirePermissionAsync(workspace,EntityType.ChatRoom, PermissionAction.Create, cancellationToken);
        var chatRoom = ChatRoom.Create(request.name, request.workspaceId, CurrentUserId, request.inviteMembersInWorkspace, request.avatarUrl);
        if (request.memberIds?.Count > 0)
        {
            var validMemberIds = await UnitOfWork.Set<WorkspaceMember>()
                .Where(wm => wm.ProjectWorkspaceId == request.workspaceId && request.memberIds.Contains(wm.UserId))
                .Select(wm => wm.UserId)
                .ToListAsync(cancellationToken);
            if (validMemberIds.Count != request.memberIds.Count)
                throw new ValidationException("Some members are not part of this workspace.");

            var membersToAdd = ChatRoomMember.AddMembers(chatRoom.Id, validMemberIds, CurrentUserId);
            chatRoom.AddMembers(membersToAdd);
        }

        await UnitOfWork.Set<ChatRoom>().AddAsync(chatRoom, cancellationToken);
        return Unit.Value;
    }
}
