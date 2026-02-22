using Application.Helpers;
using Application.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;
using MediatR;
using server.Application.Interfaces;

namespace Application.Features.ChatRoomFeatures.DeleteChatRoom;

public class DeleteChatRoomHandler : BaseFeatureHandler, IRequestHandler<DeleteChatRoomCommand, Unit>
{
    public DeleteChatRoomHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext) : base(unitOfWork, currentUserService, workspaceContext)
    {
    }

    public async Task<Unit> Handle(DeleteChatRoomCommand request, CancellationToken cancellationToken)
    {
        var chatRoom = await FindOrThrowAsync<ChatRoom>(request.ChatRoomId);
        chatRoom.SoftDelete();
        UnitOfWork.Set<ChatRoom>().Update(chatRoom);
        return Unit.Value;
    }
}
