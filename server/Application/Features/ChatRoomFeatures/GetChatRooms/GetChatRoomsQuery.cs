using Application.Common.Interfaces;

namespace Application.Features.ChatRoomFeatures.GetChatRooms;

public record GetChatRoomsQuery(Guid workspaceId) : IQueryRequest<List<ChatRoomDto>>;

public record ChatRoomDto(Guid id, string name, string? avatarUrl, bool isPrivate, bool isArchived, DateTime createdAt);
