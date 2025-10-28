using System;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services.Permissions;
using Domain.Enums;
using MediatR;
using server.Application.Interfaces;

namespace Application.Features.WorkspaceFeatures.ChatRoomManage.DeleteChatRoom;

public class DeleteChatRoomHandler : BaseCommandHandler, IRequestHandler<DeleteChatRoomCommand, Unit>
{
    public DeleteChatRoomHandler(IUnitOfWork unitOfWork, IPermissionService permissionService, ICurrentUserService currentUserService)
    : base(unitOfWork, permissionService, currentUserService) { }
    public async Task<Unit> Handle(DeleteChatRoomCommand request, CancellationToken cancellationToken)
    {
        await RequirePermissionAsync(request.workspaceId, EntityType.ChatRoom, PermissionAction.Delete, cancellationToken);
        
    }
}
