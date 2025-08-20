using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using src.Application.Common.DTOs;
using src.Domain.Entities.WorkspaceEntity.SupportEntiy;
using src.Helper.Results;
using src.Infrastructure.Data;

namespace src.Feature.ListManager.GetTaskForList
{
    public class GetTaskForListHandler : IRequestHandler<GetTaskForListRequest, Result<List<StatusColumn>, ErrorResponse>>
    {
        private readonly PlannerDbContext _context;

        public GetTaskForListHandler(PlannerDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<Result<List<StatusColumn>, ErrorResponse>> Handle(GetTaskForListRequest request, CancellationToken cancellationToken)
        {
            var spaceId = await _context.Lists
                .Where(l => l.Id == request.listId)
                .Select(l => l.SpaceId)
                .FirstOrDefaultAsync(cancellationToken);
   
            if (spaceId == Guid.Empty)
            {
                return Result<List<StatusColumn>, ErrorResponse>.Failure(
                    ErrorResponse.NotFound("The specified list was not found or is not associated with a space."));
            }

            // Single optimized query with projections
            var result = await _context.Statuses
                .Where(s => s.SpaceId == spaceId)
                .OrderBy(s => s.Type) // Assuming you have OrderIndex on Status
                .Select(s => new StatusColumn(
                    s.Id,
                    s.Name,
                    s.Color,
                    s.Type,
                    s.Tasks
                        .Where(t => t.ListId == request.listId && !t.IsArchived)
                        .OrderBy(t => t.OrderIndex)
                        .Select(t => new TaskSummary(
                            t.Id,
                            t.Name,
                            t.DueDate,
                            t.Priority,
                            t.Asignees
                                .Select(ua => new UserSummary(
                                    ua.User.Id,
                                    ua.User.Name,
                                    ua.User.Email,
                                    null 
                                ))
                                .ToList()
                        ))
                        .ToList()
                ))
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            return Result<List<StatusColumn>, ErrorResponse>.Success(result);
        }
    }
}