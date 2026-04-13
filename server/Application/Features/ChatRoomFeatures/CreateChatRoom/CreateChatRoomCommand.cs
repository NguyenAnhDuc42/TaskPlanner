using Application.Common.Interfaces;
using Application.Common.Results;
using MediatR;

namespace Application.Features.ChatRoomFeatures.CreateChatRoom;

public record CreateChatRoomCommand(
    Guid workspaceId, 
    string name, 
    string? avatarUrl = null, 
    bool inviteMembersInWorkspace = false, 
    List<Guid>? memberIds = null
) : ICommandRequest;
