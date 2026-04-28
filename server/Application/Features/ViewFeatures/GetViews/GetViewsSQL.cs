namespace Application.Features.ViewFeatures;

public static class GetViewsSQL
{
    public const string GetViews = @"
        SELECT id AS Id, name AS Name, view_type AS ViewType, is_default AS IsDefault
        FROM view_definitions
        WHERE 
            (@LayerType = 'ProjectWorkspace' AND project_workspace_id = @LayerId AND space_id IS NULL AND folder_id IS NULL) OR
            (@LayerType = 'ProjectSpace' AND space_id = @LayerId AND folder_id IS NULL) OR
            (@LayerType = 'ProjectFolder' AND folder_id = @LayerId)
        ORDER BY sort_order, created_at";
}
