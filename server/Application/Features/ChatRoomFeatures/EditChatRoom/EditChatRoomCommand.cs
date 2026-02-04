using Application.Common.Interfaces;
using MediatR;

namespace Application.Features.ChatRoomFeatures.EditChatRoom;

public record class EditChatRoomCommand(Guid ChatRoomId, string? NewName, string? AvatarUrl, bool IsPrivate, bool IsArchived, bool TurnOffNotifications) : ICommand<Unit>;
