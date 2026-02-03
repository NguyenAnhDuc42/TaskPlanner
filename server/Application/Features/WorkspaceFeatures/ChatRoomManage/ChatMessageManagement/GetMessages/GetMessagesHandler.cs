using Application.Helper;
using Application.Interfaces.Repositories;
using Domain;
using Application.Helpers;
using Domain.Entities.Relationship;
using Domain.Entities.Support.Workspace;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.WorkspaceFeatures.ChatRoomManage.ChatMessageManagement.GetMessages;

public class GetMessagesHandler : BaseQueryHandler, IRequestHandler<GetMessagesQuery, GetMessagesResult>
{
    public GetMessagesHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext, CursorHelper cursorHelper)
    : base(unitOfWork, currentUserService, workspaceContext, cursorHelper) { }

    public async Task<GetMessagesResult> Handle(GetMessagesQuery request, CancellationToken cancellationToken)
    {
        // Verify user is member of chat room
        var isMember = await UnitOfWork.Set<ChatRoomMember>()
            .AnyAsync(m => m.ChatRoomId == request.ChatRoomId && m.UserId == CurrentUserId && m.DeletedAt == null, cancellationToken);

        if (!isMember)
        {
            // Check if room is public
            var isPublic = await UnitOfWork.Set<ChatRoom>()
                .AnyAsync(cr => cr.Id == request.ChatRoomId && !cr.IsPrivate && cr.DeletedAt == null, cancellationToken);

            if (!isPublic)
                throw new UnauthorizedAccessException("You don't have access to this chat room.");
        }

        // Build query
        var query = UnitOfWork.Set<ChatMessage>()
            .AsNoTracking()
            .Where(m => m.ChatRoomId == request.ChatRoomId && m.DeletedAt == null);

        // Cursor-based pagination: get messages before a specific message
        if (request.BeforeMessageId.HasValue)
        {
            var cursorMessage = await UnitOfWork.Set<ChatMessage>()
                .Where(m => m.Id == request.BeforeMessageId.Value)
                .Select(m => m.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (cursorMessage != default)
            {
                query = query.Where(m => m.CreatedAt < cursorMessage);
            }
        }

        // Fetch one extra to check if there are more messages
        var messages = await query
            .OrderByDescending(m => m.CreatedAt)
            .Take(request.Limit + 1)
            .Select(m => new MessageDto(
                m.Id,
                m.CreatorId ?? Guid.Empty,
                m.Content,
                m.IsEdited,
                m.EditedAt,
                m.IsPinned,
                m.ReplyToMessageId,
                m.ReactionCount,
                m.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        var hasMore = messages.Count > request.Limit;
        if (hasMore)
        {
            messages = messages.Take(request.Limit).ToList();
        }

        // Reverse to get chronological order (oldest first)
        messages.Reverse();

        return new GetMessagesResult(messages, hasMore);
    }
}
