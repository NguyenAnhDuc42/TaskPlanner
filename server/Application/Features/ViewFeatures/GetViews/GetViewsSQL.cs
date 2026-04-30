namespace Application.Features.ViewFeatures;

public static class GetViewsSQL
{
    public const string GetViews = @"
        SELECT id AS Id, name AS Name, view_type AS ViewType, is_default AS IsDefault
        FROM view_definitions
        WHERE 
            (@LayerType = 'ProjectWorkspace' AND project_workspace_id = @LayerId AND project_space_id IS NULL AND project_folder_id IS NULL) OR
            (@LayerType = 'ProjectSpace' AND project_space_id = @LayerId AND project_folder_id IS NULL) OR
            (@LayerType = 'ProjectFolder' AND project_folder_id = @LayerId)
        ORDER BY order_key, created_at";
}
