using Application.Common.Interfaces;

namespace Application.Features.ChatRoomFeatures.RemoveMemberFromChatRoom;

public record RemoveMembersFromChatRoomCommand(
    Guid chatRoomId,
    List<Guid> memberIds
) : ICommandRequest;
