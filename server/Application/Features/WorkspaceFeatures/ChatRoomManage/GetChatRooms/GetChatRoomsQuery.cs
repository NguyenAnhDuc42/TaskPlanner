using Application.Common.Interfaces;

namespace Application.Features.WorkspaceFeatures.ChatRoomManage.GetChatRooms;

public record GetChatRoomsQuery(Guid WorkspaceId) : IQuery<List<ChatRoomDto>>;

public record ChatRoomDto(
    Guid Id,
    string Name,
    string? AvatarUrl,
    bool IsPrivate,
    bool IsArchived,
    int MemberCount,
    DateTimeOffset CreatedAt
);
