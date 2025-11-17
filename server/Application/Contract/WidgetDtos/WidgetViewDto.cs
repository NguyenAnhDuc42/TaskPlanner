using Domain.Enums.RelationShip;
using Domain.Enums.Widget;

namespace Application.Contract.WidgetDtos;


public class DashboardWidgetListDto
{
    public Guid DashboardId { get; set; }
    public string? Name { get; set; }
    public EntityLayerType LayerType { get; set; }
    public List<WidgetDto> Widgets { get; set; } = new();
}
public record class WidgetDto
{
    public Guid WidgetId { get; set; }
    public WidgetType Type { get; set; }
    public string? ConfigJson { get; set; }
    public object? Data { get; set; }
    public WidgetLayoutDto? Layout { get; set; }
}

public record WidgetLayoutDto
{
    public int Col { get; init; }
    public int Row { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
}
