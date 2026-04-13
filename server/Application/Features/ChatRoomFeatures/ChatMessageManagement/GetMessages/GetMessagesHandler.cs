using Application.Common.Results;
using Application.Common.Interfaces;
using Application.Helpers;
using Application.Interfaces.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.ChatRoomFeatures.ChatMessageManagement.GetMessages;

public class GetMessagesHandler(IDataBase db, WorkspaceContext context) : IQueryHandler<GetMessagesQuery, List<MessageDto>>
{
    public async Task<Result<List<MessageDto>>> Handle(GetMessagesQuery request, CancellationToken ct)
    {
        var query = db.ChatMessages
            .AsNoTracking()
            .Where(x => x.ChatRoomId == request.ChatRoomId);

        if (request.BeforeMessageId.HasValue)
        {
            var beforeMessage = await db.ChatMessages.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.BeforeMessageId, ct);
            if (beforeMessage != null)
            {
                query = query.Where(x => x.CreatedAt < beforeMessage.CreatedAt);
            }
        }

        var messages = await query
            .OrderByDescending(x => x.CreatedAt)
            .Take(request.Limit)
            .Select(x => new MessageDto(x.Id, x.CreatorId ?? Guid.Empty, x.Content, x.CreatedAt.DateTime, x.ReplyToMessageId))
            .ToListAsync(ct);

        return Result<List<MessageDto>>.Success(messages);
    }
}
