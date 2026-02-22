using Application.Helpers;
using Application.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.ChatRoomFeatures.ChatMessageManagement.GetMessages;

public class GetMessagesHandler : BaseFeatureHandler, IRequestHandler<GetMessagesQuery, List<MessageDto>>
{
    public GetMessagesHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext) : base(unitOfWork, currentUserService, workspaceContext)
    {
    }

    public async Task<List<MessageDto>> Handle(GetMessagesQuery request, CancellationToken cancellationToken)
    {
        var query = UnitOfWork.Set<ChatMessage>()
            .AsNoTracking()
            .Where(x => x.ChatRoomId == request.ChatRoomId);

        if (request.BeforeMessageId.HasValue)
        {
            var beforeMessage = await UnitOfWork.Set<ChatMessage>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.BeforeMessageId, cancellationToken);
            if (beforeMessage != null)
            {
                query = query.Where(x => x.CreatedAt < beforeMessage.CreatedAt);
            }
        }

        var messages = await query
            .OrderByDescending(x => x.CreatedAt)
            .Take(request.Limit)
            .Select(x => new MessageDto(x.Id, x.CreatorId ?? Guid.Empty, x.Content, x.CreatedAt.DateTime, x.ReplyToMessageId)) // Use CreatorId and convert to DateTime
            .ToListAsync(cancellationToken);

        return messages;
    }
}
