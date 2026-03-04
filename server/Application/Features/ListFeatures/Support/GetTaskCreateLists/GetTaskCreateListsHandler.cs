using Application.Helpers;
using Application.Interfaces.Repositories;
using Domain.Entities.ProjectEntities;
using Domain.Entities.Relationship;
using Domain.Enums.RelationShip;
using MediatR;
using Microsoft.EntityFrameworkCore;
using server.Application.Interfaces;

namespace Application.Features.ListFeatures.Support.GetTaskCreateLists;

public class GetTaskCreateListsHandler : BaseFeatureHandler, IRequestHandler<GetTaskCreateListsQuery, List<TaskCreateListOptionDto>>
{
    public GetTaskCreateListsHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        WorkspaceContext workspaceContext)
        : base(unitOfWork, currentUserService, workspaceContext) { }

    public async Task<List<TaskCreateListOptionDto>> Handle(GetTaskCreateListsQuery request, CancellationToken cancellationToken)
    {
        if (request.LayerType == EntityLayerType.ProjectWorkspace && request.LayerId != WorkspaceId)
        {
            return new List<TaskCreateListOptionDto>();
        }

        if (request.LayerType != EntityLayerType.ProjectWorkspace)
        {
            await GetLayer(request.LayerId, request.LayerType);
        }

        var workspaceMemberId = await WorkspaceContext.GetWorkspaceMemberIdAsync(cancellationToken);
        var entityAccess = UnitOfWork.Set<EntityAccess>().AsNoTracking();

        var scopedQuery =
            from l in UnitOfWork.Set<ProjectList>().AsNoTracking()
            join s in UnitOfWork.Set<ProjectSpace>().AsNoTracking() on l.ProjectSpaceId equals s.Id
            join f0 in UnitOfWork.Set<ProjectFolder>().AsNoTracking() on l.ProjectFolderId equals f0.Id into folderJoin
            from f in folderJoin.DefaultIfEmpty()
            where l.DeletedAt == null
               && s.DeletedAt == null
               && s.ProjectWorkspaceId == WorkspaceId
               && (f == null || f.DeletedAt == null)
            select new { l, s, f };

        scopedQuery = request.LayerType switch
        {
            EntityLayerType.ProjectWorkspace => scopedQuery,
            EntityLayerType.ProjectSpace => scopedQuery.Where(x => x.s.Id == request.LayerId),
            EntityLayerType.ProjectFolder => scopedQuery.Where(x => x.f != null && x.f.Id == request.LayerId),
            EntityLayerType.ProjectList => scopedQuery.Where(x => x.l.Id == request.LayerId),
            _ => throw new ArgumentOutOfRangeException(nameof(request.LayerType), request.LayerType, "Unsupported layer type.")
        };

        var visibleListCandidates = await scopedQuery
            .Where(x =>
                (!x.s.IsPrivate || entityAccess.Any(ea =>
                    ea.EntityId == x.s.Id &&
                    ea.EntityLayer == EntityLayerType.ProjectSpace &&
                    ea.WorkspaceMemberId == workspaceMemberId &&
                    ea.DeletedAt == null))
                &&
                (x.f == null || !x.f.IsPrivate || entityAccess.Any(ea =>
                    ea.EntityId == x.f.Id &&
                    ea.EntityLayer == EntityLayerType.ProjectFolder &&
                    ea.WorkspaceMemberId == workspaceMemberId &&
                    ea.DeletedAt == null))
                &&
                (!x.l.IsPrivate || entityAccess.Any(ea =>
                    ea.EntityId == x.l.Id &&
                    ea.EntityLayer == EntityLayerType.ProjectList &&
                    ea.WorkspaceMemberId == workspaceMemberId &&
                    ea.DeletedAt == null)))
            .Select(x => new
            {
                x.l.Id,
                x.l.Name,
                x.l.Customization.Color,
                x.l.Customization.Icon,
                EffectiveLayerId = !x.l.InheritStatus
                    ? x.l.Id
                    : x.f != null && !x.f.InheritStatus
                        ? x.f.Id
                        : x.s.Id,
                EffectiveLayerType = !x.l.InheritStatus
                    ? EntityLayerType.ProjectList
                    : x.f != null && !x.f.InheritStatus
                        ? EntityLayerType.ProjectFolder
                        : EntityLayerType.ProjectSpace
            })
            .ToListAsync(cancellationToken);

        if (request.StatusId.HasValue)
        {
            var statusLayer = await UnitOfWork.Set<Status>()
                .AsNoTracking()
                .Where(s => s.Id == request.StatusId.Value && s.DeletedAt == null && s.LayerId != null)
                .Select(s => new
                {
                    LayerId = s.LayerId!.Value,
                    s.LayerType
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (statusLayer == null)
            {
                return new List<TaskCreateListOptionDto>();
            }

            visibleListCandidates = visibleListCandidates
                .Where(x =>
                    x.EffectiveLayerId == statusLayer.LayerId &&
                    x.EffectiveLayerType == statusLayer.LayerType)
                .ToList();
        }

        return visibleListCandidates
            .OrderBy(x => x.Name)
            .Select(x => new TaskCreateListOptionDto(
                x.Id,
                x.Name,
                x.Color,
                x.Icon))
            .ToList();
    }
}
