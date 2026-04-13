using Application.Common.Interfaces;

namespace Application.Features.ChatRoomFeatures.InviteMemberToChatRoom;

public record InviteMembersToChatRoomCommand(
    Guid chatRoomId,
    List<Guid> memberIds
) : ICommandRequest;
