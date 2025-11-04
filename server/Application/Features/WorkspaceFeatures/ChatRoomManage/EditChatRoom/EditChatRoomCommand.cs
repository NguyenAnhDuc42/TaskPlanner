using Application.Common.Interfaces;
using Domain.Enums.Workspace;
using MediatR;

namespace Application.Features.WorkspaceFeatures.ChatRoomManage.EditChatRoom;

public record class EditChatRoomCommand
(Guid chatRoomId, string newName, string? avatarUrl, ChatRoomType chatRoomType, bool isPrivate, bool isArchived, bool turnOffNotifications) : ICommand<Unit>;
