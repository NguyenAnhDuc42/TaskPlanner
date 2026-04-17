using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Helpers;
using Application.Interfaces.Data;
using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using Dapper;

namespace Application.Features.WorkspaceFeatures.HierarchyManagement.GetHierarchy;

public class GetHierarchyHandler(IDataBase db, WorkspaceContext context) : IQueryHandler<GetHierarchyQuery, WorkspaceHierarchyDto>
{
    public async Task<Result<WorkspaceHierarchyDto>> Handle(GetHierarchyQuery request, CancellationToken ct)
    {
        // FIX: One query instead of two separate DB round-trips.
        // The workspace name comes back on every row; we read it once.
        var rows = (await db.QueryAsync<SpaceRow>(
            GetHierarchySql.GetSpacesAndWorkspaceQuery,
            new { WorkspaceId = request.WorkspaceId },
            cancellationToken: ct)).AsList();

        if (rows.Count == 0)
            return Result<WorkspaceHierarchyDto>.Failure(
                Error.NotFound("Workspace.NotFound",
                    $"Workspace {request.WorkspaceId} not found"));

        return Result<WorkspaceHierarchyDto>.Success(BuildHierarchy(rows));
    }

    private static WorkspaceHierarchyDto BuildHierarchy(List<SpaceRow> rows)
    {
        var spaces = new List<SpaceHierarchyDto>(rows.Count);

        foreach (var s in rows)
        {
            spaces.Add(new SpaceHierarchyDto
            {
                Id        = s.Id,
                Name      = s.Name,
                Color     = s.Color  ?? string.Empty,
                Icon      = s.Icon   ?? string.Empty,
                IsPrivate = s.IsPrivate,
                OrderKey  = s.OrderKey,
                HasFolders = s.HasFolders,
                HasTasks   = s.HasTasks,
                Folders   = new List<FolderHierarchyDto>(0),
                Tasks     = new List<TaskHierarchyDto>(0)
            });
        }

        return new WorkspaceHierarchyDto
        {
            Name   = rows[0].WorkspaceName,
            Spaces = spaces
        };
    }

    // WorkspaceName comes back on every row — tiny overhead, huge simplification.
    private record SpaceRow(
        string WorkspaceName,
        Guid Id, string Name, string? Color, string? Icon,
        bool IsPrivate, string OrderKey,
        bool HasFolders, bool HasTasks);
}
