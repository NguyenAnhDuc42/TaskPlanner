using Application.Common.Interfaces;
using MediatR;

namespace Application.Features.ChatRoomFeatures.DeleteChatRoom;

public record class DeleteChatRoomCommand(Guid ChatRoomId) : ICommand<Unit>;
