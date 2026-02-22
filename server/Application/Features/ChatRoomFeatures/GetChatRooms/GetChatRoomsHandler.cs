using Application.Helpers;
using Application.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.ChatRoomFeatures.GetChatRooms;

public class GetChatRoomsHandler : BaseFeatureHandler, IRequestHandler<GetChatRoomsQuery, List<ChatRoomDto>>
{
    public GetChatRoomsHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext) : base(unitOfWork, currentUserService, workspaceContext)
    {
    }

    public async Task<List<ChatRoomDto>> Handle(GetChatRoomsQuery request, CancellationToken cancellationToken)
    {
        var chatRooms = await UnitOfWork.Set<ChatRoom>()
            .AsNoTracking()
            .Where(x => x.ProjectWorkspaceId == request.workspaceId && x.DeletedAt == null)
            .OrderBy(x => x.CreatedAt)
            .Select(x => new ChatRoomDto(x.Id, x.Name, x.AvatarUrl, x.IsPrivate, x.IsArchived, x.CreatedAt.DateTime)) // Convert DateTimeOffset to DateTime
            .ToListAsync(cancellationToken);

        return chatRooms;
    }
}
