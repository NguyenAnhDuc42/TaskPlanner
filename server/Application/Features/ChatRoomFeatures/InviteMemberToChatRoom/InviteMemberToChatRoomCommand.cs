using Application.Common.Interfaces;
using MediatR;

namespace Application.Features.ChatRoomFeatures.InviteMemberToChatRoom;

public record class InviteMembersToChatRoomCommand(
    Guid chatRoomId,
    List<Guid> memberIds
) : ICommand<Unit>;
