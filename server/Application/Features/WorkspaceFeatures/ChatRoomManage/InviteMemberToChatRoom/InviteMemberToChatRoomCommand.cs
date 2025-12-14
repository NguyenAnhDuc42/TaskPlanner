using Application.Common.Interfaces;
using MediatR;

namespace Application.Features.WorkspaceFeatures.ChatRoomManage.InviteMembersToChatRoom;

public record class InviteMembersToChatRoomCommand(
    Guid chatRoomId,
    List<Guid> memberIds
) : ICommand<Unit>;
