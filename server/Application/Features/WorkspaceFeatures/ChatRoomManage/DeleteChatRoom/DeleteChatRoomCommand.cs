using Application.Common.Interfaces;
using MediatR;

namespace Application.Features.WorkspaceFeatures.ChatRoomManage.DeleteChatRoom;

public record class DeleteChatRoomCommand(Guid workspaceId,Guid chatRoomId) : ICommand<Unit>;
