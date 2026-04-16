using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Helpers;
using Application.Interfaces.Data;
using Microsoft.EntityFrameworkCore;
using Domain.Entities;

namespace Application.Features.WorkspaceFeatures.HierarchyManagement.GetHierarchy;

public class GetHierarchyHandler(IDataBase db, WorkspaceContext context) : IQueryHandler<GetHierarchyQuery, WorkspaceHierarchyDto>
{
    public async Task<Result<WorkspaceHierarchyDto>> Handle(GetHierarchyQuery request, CancellationToken ct)
    {
        // Don't load the full entity, just the metadata we need
        var workspace = await db.Workspaces
            .AsNoTracking()
            .WhereNotDeleted()
            .Select(x => new { x.Id, x.Name })
            .FirstOrDefaultAsync(x => x.Id == request.WorkspaceId, ct);

        if (workspace == null) 
            return Result<WorkspaceHierarchyDto>.Failure(Error.NotFound("Workspace.NotFound", $"Workspace {request.WorkspaceId} not found"));

        // Fetch ONLY spaces now. Folders are lazy-loaded on expansion.
        var spaces = (await db.QueryAsync<SpaceRaw>(
            GetHierarchySql.GetSpacesOnlyQuery, 
            new { WorkspaceId = request.WorkspaceId }, 
            cancellationToken: ct)).ToList();

        return Result<WorkspaceHierarchyDto>.Success(BuildHierarchy(workspace.Name, spaces));
    }

    private static WorkspaceHierarchyDto BuildHierarchy(string workspaceName, List<SpaceRaw> spaces)
    {
        // Pre-size based exactly on the Space count
        var spaceDtos = new List<SpaceHierarchyDto>(spaces.Count);

        foreach (var s in spaces)
        {
            spaceDtos.Add(new SpaceHierarchyDto
            {
                Id = s.Id,
                Name = s.Name,
                Color = s.Color ?? string.Empty,
                Icon = s.Icon ?? string.Empty,
                IsPrivate = s.IsPrivate,
                OrderKey = s.OrderKey,
                HasFolders = s.HasFolders != 0,
                HasTasks = s.HasTasks != 0,
                Folders = new List<FolderHierarchyDto>(0), // Start empty, lazy-loaded
                Tasks = new()
            });
        }

        return new WorkspaceHierarchyDto
        {
            Name = workspaceName,
            Spaces = spaceDtos
        };
    }

    private record SpaceRaw(Guid Id, string Name, string? Color, string? Icon, bool IsPrivate, string OrderKey, int HasFolders, int HasTasks);
}
