using Application.Common.Interfaces;

namespace Application.Features.ChatRoomFeatures.EditChatRoom;

public record EditChatRoomCommand(
    Guid ChatRoomId, 
    string? NewName, 
    string? AvatarUrl, 
    bool IsPrivate, 
    bool IsArchived, 
    bool TurnOffNotifications
) : ICommandRequest;
