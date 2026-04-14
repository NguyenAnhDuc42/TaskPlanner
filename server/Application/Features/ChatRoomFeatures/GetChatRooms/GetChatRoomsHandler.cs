using Application.Common.Results;
using Application.Common.Interfaces;
using Application.Helpers;
using Application.Interfaces.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.ChatRoomFeatures.GetChatRooms;

public class GetChatRoomsHandler(IDataBase db) : IQueryHandler<GetChatRoomsQuery, List<ChatRoomDto>>
{
    public async Task<Result<List<ChatRoomDto>>> Handle(GetChatRoomsQuery request, CancellationToken ct)
    {
        var chatRooms = await db.ChatRooms
            .AsNoTracking()
            .Where(x => x.ProjectWorkspaceId == request.workspaceId && x.DeletedAt == null)
            .OrderBy(x => x.CreatedAt)
            .Select(x => new ChatRoomDto(
                x.Id, 
                x.Name, 
                x.AvatarUrl, 
                x.IsPrivate, 
                x.IsArchived, 
                x.CreatedAt.DateTime))
            .ToListAsync(ct);

        return Result<List<ChatRoomDto>>.Success(chatRooms);
    }
}
