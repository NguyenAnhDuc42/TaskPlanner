namespace Application.Features.ViewFeatures;

public static class GetViewsSQL
{
    public const string GetViews = @"
        SELECT id AS Id, name AS Name, view_type AS ViewType, is_default AS IsDefault
        FROM view_definitions
        WHERE layer_id = @LayerId AND layer_type = @LayerType
        ORDER BY sort_order, created_at";
}
