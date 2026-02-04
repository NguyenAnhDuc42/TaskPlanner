using Application.Common.Interfaces;
using MediatR;

namespace Application.Features.ChatRoomFeatures.CreateChatRoom;

public record class CreateChatRoomCommand(Guid workspaceId,string name,string ?avatarUrl = null,bool inviteMembersInWorkspace = false,List<Guid>? memberIds = null) : ICommand<Unit>;
