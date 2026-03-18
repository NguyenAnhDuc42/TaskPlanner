using Application.Features.DashboardFeatures.CreateDashboard;
using Application.Features.DashboardFeatures.CreateWidget;
using Application.Features.DashboardFeatures.DeleteDashboard;
using Application.Features.DashboardFeatures.DeleteWidget;
using Application.Features.DashboardFeatures.EditDashboard;
using Application.Features.DashboardFeatures.EditWidget;
using Application.Features.DashboardFeatures.GetDashboardList;
using Application.Features.DashboardFeatures.GetWidgetList;
using Application.Features.DashboardFeatures.SaveDashboardLayout;
using Domain.Enums.RelationShip;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class DashboardsController : ControllerBase
{
    private readonly IMediator _mediator;

    public DashboardsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> CreateDashboard([FromBody] CreateDashboardCommand command, CancellationToken ct)
    {
        await _mediator.Send(command, ct);
        return Ok();
    }

    [HttpGet]
    public async Task<ActionResult<List<DashboardDto>>> GetDashboards(
        [FromQuery] Guid layerId, 
        [FromQuery] EntityLayerType layerType, 
        CancellationToken ct)
    {
        var result = await _mediator.Send(new GetDashboardListQuery(layerId, layerType), ct);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateDashboard(Guid id, [FromBody] EditDashboardCommand command, CancellationToken ct)
    {
        // Ensure ID consistency if provided in body
        var finalCommand = command with { dashboardId = id };
        await _mediator.Send(finalCommand, ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteDashboard(Guid id, [FromQuery] Guid layerId, [FromQuery] EntityLayerType layerType, CancellationToken ct)
    {
        await _mediator.Send(new DeleteDashboardCommand(layerId, layerType, id), ct);
        return NoContent();
    }

    [HttpGet("{id:guid}/widgets")]
    public async Task<ActionResult<List<WidgetDto>>> GetWidgets(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetWidgetListQuery(id), ct);
        return Ok(result);
    }

    [HttpPost("{id:guid}/widgets")]
    public async Task<IActionResult> CreateWidget(Guid id, [FromBody] CreateWidgetCommand command, CancellationToken ct)
    {
        var finalCommand = command with { dashboardId = id };
        await _mediator.Send(finalCommand, ct);
        return Ok();
    }

    [HttpPut("{id:guid}/widgets/{widgetId:guid}")]
    public async Task<IActionResult> UpdateWidget(Guid id, Guid widgetId, [FromBody] EditWidgetCommand command, CancellationToken ct)
    {
        var finalCommand = command with { dashboardId = id, widgetId = widgetId };
        await _mediator.Send(finalCommand, ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}/widgets/{widgetId:guid}")]
    public async Task<IActionResult> DeleteWidget(Guid id, Guid widgetId, CancellationToken ct)
    {
        await _mediator.Send(new DeleteWidgetCommand(id, widgetId), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/layout")]
    public async Task<IActionResult> SaveLayout(Guid id, [FromBody] List<WidgetLayoutUpdateDto> layouts, CancellationToken ct)
    {
        var result = await _mediator.Send(new SaveDashboardLayoutCommand(id, layouts), ct);
        return result ? Ok() : NotFound();
    }
}
