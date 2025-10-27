using Application.Common.Interfaces;
using MediatR;

namespace Application.Features.WorkspaceFeatures.ChatRoomManage.CreateChatRoom;

public record class CreateChatRoomCommand(Guid workspaceId,string name,string ?avatarUrl = null,bool inviteMembersInWorkspace = false,List<Guid>? memberIds = null) : ICommand<Unit>;
