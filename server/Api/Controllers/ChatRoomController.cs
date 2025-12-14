using Application.Features.WorkspaceFeatures.ChatRoomManage.ChatMessageManagement.GetMessages;
using Application.Features.WorkspaceFeatures.ChatRoomManage.ChatMessageManagement.SendMessage;
using Application.Features.WorkspaceFeatures.ChatRoomManage.CreateChatRoom;
using Application.Features.WorkspaceFeatures.ChatRoomManage.DeleteChatRoom;
using Application.Features.WorkspaceFeatures.ChatRoomManage.EditChatRoom;
using Application.Features.WorkspaceFeatures.ChatRoomManage.GetChatRooms;
using Application.Features.WorkspaceFeatures.ChatRoomManage.InviteMembersToChatRoom;
using Application.Features.WorkspaceFeatures.ChatRoomManage.RemoveMembersFromChatRoom;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;
[Route("api/[controller]")]
[ApiController]
public class ChatRoomController : ControllerBase
{
    private readonly IMediator _mediator;
    public ChatRoomController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("workspaces/{workspaceId:guid}/chat-rooms")]
    public async Task<IActionResult> GetChatRooms(Guid workspaceId, CancellationToken cancellationToken)
    {
        var query = new GetChatRoomsQuery(workspaceId);
        return await SendRequest(query, cancellationToken);
    }

    [HttpPost("workspaces/{workspaceId:guid}/chat-rooms")]
    public async Task<IActionResult> CreateChatRoom(Guid workspaceId, [FromBody] CreateChatRoomRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateChatRoomCommand(workspaceId, request.Name, request.AvatarUrl, request.InviteMembersInWorkspace, request.MemberIds);
        return await SendRequest(command, cancellationToken);
    }

    [HttpDelete("chat-rooms/{chatRoomId:guid}")]
    public async Task<IActionResult> DeleteChatRoom(Guid chatRoomId, CancellationToken cancellationToken)
    {
        var command = new DeleteChatRoomCommand(chatRoomId);
        return await SendRequest(command, cancellationToken);
    }

    [HttpPut("chat-rooms/{chatRoomId:guid}")]
    public async Task<IActionResult> EditChatRoom(Guid chatRoomId, [FromBody] EditChatRoomRequest request, CancellationToken cancellationToken)
    {
        var command = new EditChatRoomCommand(chatRoomId, request.NewName, request.AvatarUrl, request.IsPrivate, request.IsArchived, request.TurnOffNotifications);
        return await SendRequest(command, cancellationToken);
    }

    [HttpPost("chat-rooms/{chatRoomId:guid}/members")]
    public async Task<IActionResult> InviteMembers(Guid chatRoomId, [FromBody] InviteMembersRequest request, CancellationToken cancellationToken)
    {
        var command = new InviteMembersToChatRoomCommand(chatRoomId, request.MemberIds);
        return await SendRequest(command, cancellationToken);
    }

    [HttpDelete("chat-rooms/{chatRoomId:guid}/members")]
    public async Task<IActionResult> RemoveMembers(Guid chatRoomId, [FromBody] RemoveMembersRequest request, CancellationToken cancellationToken)
    {
        var command = new RemoveMembersFromChatRoomCommand(chatRoomId, request.MemberIds);
        return await SendRequest(command, cancellationToken);
    }

    [HttpGet("chat-rooms/{chatRoomId:guid}/messages")]
    public async Task<IActionResult> GetMessages(
        Guid chatRoomId,
        [FromQuery] int limit = 50,
        [FromQuery] Guid? beforeMessageId = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetMessagesQuery(chatRoomId, limit, beforeMessageId);
        return await SendRequest(query, cancellationToken);
    }

    [HttpPost("chat-rooms/{chatRoomId:guid}/messages")]
    public async Task<IActionResult> SendMessage(Guid chatRoomId, [FromBody] SendMessageRequest request, CancellationToken cancellationToken)
    {
        var command = new SendMessageCommand(chatRoomId, request.Content, request.ReplyToMessageId);
        return await SendRequest(command, cancellationToken);
    }

    // Helper
    private async Task<IActionResult> SendRequest<T>(IRequest<T> request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(request, cancellationToken);
        return Ok(result);
    }
}

// Request Models
public record CreateChatRoomRequest(string Name, string? AvatarUrl, bool InviteMembersInWorkspace, List<Guid>? MemberIds);
public record EditChatRoomRequest(string? NewName, string? AvatarUrl, bool IsPrivate, bool IsArchived, bool TurnOffNotifications);
public record InviteMembersRequest(List<Guid> MemberIds);
public record RemoveMembersRequest(List<Guid> MemberIds);
public record SendMessageRequest(string Content, Guid? ReplyToMessageId);
