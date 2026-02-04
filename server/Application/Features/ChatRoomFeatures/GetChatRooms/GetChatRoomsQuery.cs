using MediatR;

namespace Application.Features.ChatRoomFeatures.GetChatRooms;

public record class GetChatRoomsQuery(Guid workspaceId) : IRequest<List<ChatRoomDto>>;

public record class ChatRoomDto(Guid id, string name, string? avatarUrl, bool isPrivate, bool isArchived, DateTime createdAt);
