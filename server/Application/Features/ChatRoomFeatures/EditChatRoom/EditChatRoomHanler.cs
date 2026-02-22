using Application.Helpers;
using Application.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;
using MediatR;
using server.Application.Interfaces;

namespace Application.Features.ChatRoomFeatures.EditChatRoom;

public class EditChatRoomHanler : BaseFeatureHandler, IRequestHandler<EditChatRoomCommand, Unit>
{
    public EditChatRoomHanler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext) : base(unitOfWork, currentUserService, workspaceContext)
    {
    }

    public async Task<Unit> Handle(EditChatRoomCommand request, CancellationToken cancellationToken)
    {
        var chatRoom = await FindOrThrowAsync<ChatRoom>(request.ChatRoomId);
        chatRoom.Update(request.NewName, request.AvatarUrl, request.IsPrivate, request.IsArchived);
        UnitOfWork.Set<ChatRoom>().Update(chatRoom);
        return Unit.Value;
    }
}
