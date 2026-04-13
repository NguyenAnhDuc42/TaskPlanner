using Application.Common.Interfaces;

namespace Application.Features.ChatRoomFeatures.DeleteChatRoom;

public record DeleteChatRoomCommand(Guid ChatRoomId) : ICommandRequest;
