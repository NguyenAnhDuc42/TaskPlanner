using Application.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;
using MediatR;
using server.Application.Interfaces;
using Application.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Application.Features.StatusManagement.SyncStatuses;

public class SyncStatusesHandler : BaseFeatureHandler, IRequestHandler<SyncStatusesCommand, Unit>
{
    public SyncStatusesHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, WorkspaceContext workspaceContext)
        : base(unitOfWork, currentUserService, workspaceContext) { }

    public async Task<Unit> Handle(SyncStatusesCommand request, CancellationToken cancellationToken)
    {
        await GetLayer(request.LayerId, request.LayerType);

        var existingStatuses = await UnitOfWork.Set<Status>()
            .Where(s => s.LayerId == request.LayerId && s.LayerType == request.LayerType)
            .ToListAsync(cancellationToken);

        foreach (var item in request.Statuses)
        {
            if (item.Id == null || item.Id == Guid.Empty)
            {
                // Create new
                if (item.IsDeleted) continue;

                var newStatus = Status.Create(
                    request.LayerId,
                    request.LayerType,
                    item.Name,
                    item.Color,
                    item.Category,
                    CurrentUserId
                );
                await UnitOfWork.Set<Status>().AddAsync(newStatus, cancellationToken);
            }
            else
            {
                // Find existing
                var status = existingStatuses.FirstOrDefault(s => s.Id == item.Id);
                if (status == null) continue;

                if (item.IsDeleted)
                {
                    status.SoftDelete();
                }
                else
                {
                    status.UpdateDetails(item.Name, item.Color, item.Category);
                }
            }
        }

        // Optional: Reconcile statuses not in the request (if we want "Complete Overwrite" behavior)
        // For now, explicit IsDeleted is safer for the UI flow.

        return Unit.Value;
    }
}
