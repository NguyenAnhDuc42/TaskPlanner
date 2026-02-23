using Application.Interfaces.Repositories;
using Domain;
using Application.Helpers;
using Domain.Entities.ProjectEntities;
using Domain.Enums;
using MediatR;
using server.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.StatusManagement.DeleteStatus;

public class DeleteStatusHandler : BaseFeatureHandler, IRequestHandler<DeleteStatusCommand, Unit>
{
    public DeleteStatusHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, currentUserService, workspaceContext) { }

    public async Task<Unit> Handle(DeleteStatusCommand request, CancellationToken cancellationToken)
    {
        var status = await FindOrThrowAsync<Status>(request.StatusId);
        
        // Block deletion if it's the last status in its critical category for this layer
        if (status.Category == StatusCategory.NotStarted || status.Category == StatusCategory.Done)
        {
            var otherStatusesInCategory = await UnitOfWork.Set<Status>()
                .CountAsync(s => s.LayerId == status.LayerId &&
                                 s.LayerType == status.LayerType && 
                                 s.Category == status.Category && 
                                 s.DeletedAt == null &&
                                 s.Id != status.Id,
                                 cancellationToken);
                                 
            if (otherStatusesInCategory == 0)
            {
                throw new InvalidOperationException($"Cannot delete the last status in the '{status.Category}' category. Every entity must have at least one starting and ending status.");
            }
        }

        status.SoftDelete();
        
        return Unit.Value;
    }
}
