using Application.Common.Interfaces;
using MediatR;

namespace Application.Features.WorkspaceFeatures.ChatRoomManage.RemoveMembersFromChatRoom;

public record class RemoveMembersFromChatRoomCommand(
    Guid chatRoomId,
    List<Guid> memberIds
) : ICommand<Unit>;
