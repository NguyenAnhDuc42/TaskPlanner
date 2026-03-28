using Application.Interfaces.Repositories;
using Dapper;
using Domain.Enums.RelationShip;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Features.DashboardFeatures.WidgetDataHelper.SQL;

public record class FolderListItem(
    Guid Id, 
    Guid ProjectSpaceId, 
    string SpaceName, 
    string Name, 
    string Icon, 
    string Color
);

public static class FolderListWidgetSQL
{
    public static async Task<List<FolderListItem>> ExecuteAsync(IUnitOfWork unitOfWork, Guid layerId, EntityLayerType layerType, string configJson,Guid workspaceMemberId, CancellationToken ct)
    {
        var builder = new SqlBuilder();
        var selector = builder.AddTemplate(@"
            SELECT 
                f.id,
                f.project_space_id as ProjectSpaceId,
                s.name as SpaceName,
                f.name,
                f.custom_icon as Icon,
                f.custom_color as Color
            FROM project_folder f
            INNER JOIN project_spaces s ON f.project_space_id = s.id   
            INNER JOIN project_workspaces w ON s.project_workspace_id = w.id
            /**where**/
            ORDER BY f.created_at DESC
            LIMIT @limit");

        switch (layerType)
        {
            case EntityLayerType.ProjectWorkspace:
                builder.Where("w.id = @layerId");
                break;
            case EntityLayerType.ProjectSpace:
                builder.Where("s.id = @layerId");
                break;
        }

        builder.Where(@"
            f.deleted_at IS NULL
            AND s.deleted_at IS NULL
            AND w.deleted_at IS NULL
        ");


        var parameters = new DynamicParameters();
        parameters.Add("@layerId", layerId);
        parameters.Add("@limit", 50);
        parameters.Add("@workspaceMemberId", workspaceMemberId);

        var folders = await unitOfWork.QueryAsync<FolderListItem>(selector.RawSql, parameters, ct);
        return folders.ToList();
    }
}
