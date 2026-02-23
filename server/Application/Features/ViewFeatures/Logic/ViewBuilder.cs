using Domain.Enums;
using Domain.Enums.RelationShip;

namespace Application.Features.ViewFeatures.Logic;

public class ViewBuilder
{
    private readonly IServiceProvider _serviceProvider;

    public ViewBuilder(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<object> Build(Guid layerId, EntityLayerType layerType, ViewType viewType)
    {
        return viewType switch
        {
            ViewType.List or ViewType.Board => await TaskSql.Execute(_serviceProvider, layerId, layerType),
            ViewType.Dashboard => await DashboardSql.Execute(_serviceProvider, layerId),
            ViewType.Doc => await DocumentSql.Execute(_serviceProvider, layerId),
            _ => throw new NotSupportedException($"ViewType {viewType} is not supported yet.")
        };
    }
}
