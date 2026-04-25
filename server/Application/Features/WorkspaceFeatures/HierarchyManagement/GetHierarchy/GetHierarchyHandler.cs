using Application.Common.Errors;
using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Helpers;
using Application.Interfaces.Data;
using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using Dapper;

namespace Application.Features.WorkspaceFeatures;

public class GetHierarchyHandler(IDataBase db) : IQueryHandler<GetHierarchyQuery, WorkspaceHierarchyDto>
{
    public async Task<Result<WorkspaceHierarchyDto>> Handle(GetHierarchyQuery request, CancellationToken ct)
    {
        var rows = (await db.Connection.QueryAsync<SpaceRow>(
            GetHierarchySql.GetSpacesAndWorkspaceQuery,
            new { WorkspaceId = request.WorkspaceId })).AsList();

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

    private record SpaceRow(
        string WorkspaceName,
        Guid Id, string Name, string? Color, string? Icon,
        bool IsPrivate, string OrderKey,
        bool HasFolders, bool HasTasks);
}
