using Application.Common.Interfaces;
using MediatR;

namespace Application.Features.ChatRoomFeatures.RemoveMemberFromChatRoom;

public record class RemoveMembersFromChatRoomCommand(
    Guid chatRoomId,
    List<Guid> memberIds
) : ICommand<Unit>;
