using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Interfaces.Data;
using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;

namespace Application.Features.WorkspaceFeatures.HierarchyManagement.GetHierarchy;

public record GetNodeFoldersBatchQuery(Guid WorkspaceId, IEnumerable<Guid> SpaceIds)
    : IQueryRequest<Dictionary<Guid, List<FolderHierarchyDto>>>, IAuthorizedWorkspaceRequest;

// New batch query — one call for multiple spaces at once.
// Use when the client expands several spaces simultaneously.
public class GetNodeFoldersBatchHandler(IDataBase db)
    : IQueryHandler<GetNodeFoldersBatchQuery, Dictionary<Guid, List<FolderHierarchyDto>>>
{
    public async Task<Result<Dictionary<Guid, List<FolderHierarchyDto>>>> Handle(
        GetNodeFoldersBatchQuery request, CancellationToken ct)
    {
        var spaceIdsArray = request.SpaceIds as Guid[] ?? request.SpaceIds.ToArray();

        var rawFolders = (await db.QueryAsync<FolderRow>(
            GetHierarchySql.GetFoldersByWorkspaceQuery,
            new { SpaceIds = spaceIdsArray },
            cancellationToken: ct)).AsList();

        // Group client-side — one allocation, no extra queries.
        var result = new Dictionary<Guid, List<FolderHierarchyDto>>(spaceIdsArray.Length);

        foreach (var f in rawFolders)
        {
            if (!result.TryGetValue(f.SpaceId, out var list))
                result[f.SpaceId] = list = new List<FolderHierarchyDto>();

            list.Add(new FolderHierarchyDto
            {
                Id        = f.Id,
                Name      = f.Name,
                Color     = f.Color  ?? string.Empty,
                Icon      = f.Icon   ?? string.Empty,
                IsPrivate = f.IsPrivate,
                OrderKey  = f.OrderKey,
                HasTasks  = f.HasTasks,
                Tasks     = new List<TaskHierarchyDto>(0)
            });
        }

        return Result<Dictionary<Guid, List<FolderHierarchyDto>>>.Success(result);
    }

    private record FolderRow(
        Guid Id, Guid SpaceId, string Name, string? Color, string? Icon,
        bool IsPrivate, string OrderKey, bool HasTasks);
}
