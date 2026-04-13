using Application.Features.ChatRoomFeatures.ChatMessageManagement.GetMessages;
using Application.Features.ChatRoomFeatures.ChatMessageManagement.SendMessage;
using Application.Features.ChatRoomFeatures.CreateChatRoom;
using Application.Features.ChatRoomFeatures.DeleteChatRoom;
using Application.Features.ChatRoomFeatures.EditChatRoom;
using Application.Features.ChatRoomFeatures.GetChatRooms;
using Application.Features.ChatRoomFeatures.InviteMemberToChatRoom;
using Microsoft.AspNetCore.Mvc;
using Application.Common.Interfaces;
using Api.Extensions;

namespace Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ChatRoomController : ControllerBase
{
    private readonly IHandler _handler;

    public ChatRoomController(IHandler handler)
    {
        _handler = handler;
    }

    [HttpGet("workspaces/{workspaceId:guid}/chat-rooms")]
    public async Task<IActionResult> GetChatRooms(Guid workspaceId, CancellationToken cancellationToken)
    {
        var query = new GetChatRoomsQuery(workspaceId);
        var result = await _handler.QueryAsync<GetChatRoomsQuery, List<ChatRoomDto>>(query, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("workspaces/{workspaceId:guid}/chat-rooms")]
    public async Task<IActionResult> CreateChatRoom(Guid workspaceId, [FromBody] CreateChatRoomRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateChatRoomCommand(workspaceId, request.Name, request.AvatarUrl, request.InviteMembersInWorkspace, request.MemberIds);
        var result = await _handler.SendAsync(command, cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("chat-rooms/{chatRoomId:guid}")]
    public async Task<IActionResult> DeleteChatRoom(Guid chatRoomId, CancellationToken cancellationToken)
    {
        var command = new DeleteChatRoomCommand(chatRoomId);
        var result = await _handler.SendAsync(command, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPut("chat-rooms/{chatRoomId:guid}")]
    public async Task<IActionResult> EditChatRoom(Guid chatRoomId, [FromBody] EditChatRoomRequest request, CancellationToken cancellationToken)
    {
        var command = new EditChatRoomCommand(chatRoomId, request.NewName, request.AvatarUrl, request.IsPrivate, request.IsAchived, request.TurnOffNotifications);
        var result = await _handler.SendAsync(command, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("chat-rooms/{chatRoomId:guid}/members")]
    public async Task<IActionResult> InviteMembers(Guid chatRoomId, [FromBody] InviteMembersRequest request, CancellationToken cancellationToken)
    {
        var command = new InviteMembersToChatRoomCommand(chatRoomId, request.MemberIds);
        var result = await _handler.SendAsync(command, cancellationToken);
        return result.ToActionResult();
    }


    [HttpGet("chat-rooms/{chatRoomId:guid}/messages")]
    public async Task<IActionResult> GetMessages(
        Guid chatRoomId,
        [FromQuery] int limit = 50,
        [FromQuery] Guid? beforeMessageId = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetMessagesQuery(chatRoomId, limit, beforeMessageId);
        var result = await _handler.QueryAsync<GetMessagesQuery, List<MessageDto>>(query, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("chat-rooms/{chatRoomId:guid}/messages")]
    public async Task<IActionResult> SendMessage(Guid chatRoomId, [FromBody] SendMessageRequest request, CancellationToken cancellationToken)
    {
        var command = new SendMessageCommand(chatRoomId, request.Content, request.ReplyToMessageId);
        var result = await _handler.SendAsync(command, cancellationToken);
        return result.ToActionResult();
    }
}

// Request Models
public record CreateChatRoomRequest(string Name, string? AvatarUrl, bool InviteMembersInWorkspace, List<Guid>? MemberIds);
public record EditChatRoomRequest(string? NewName, string? AvatarUrl, bool IsPrivate, bool IsAchived, bool TurnOffNotifications);
public record InviteMembersRequest(List<Guid> MemberIds);
public record SendMessageRequest(string Content, Guid? ReplyToMessageId);
