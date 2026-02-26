using Application.Features.ViewFeatures.CreateView;
using Application.Features.ViewFeatures.DeleteView;
using Application.Features.ViewFeatures.GetViewData;
using Application.Features.ViewFeatures.GetViews;
using Application.Features.ViewFeatures.UpdateView;
using Domain.Enums.RelationShip;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ViewsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ViewsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetViews(
        [FromQuery] Guid layerId,
        [FromQuery] EntityLayerType layerType,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetViewsQuery(layerId, layerType), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}/data")]
    public async Task<IActionResult> GetViewData(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetViewDataQuery(id), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateView(
        [FromBody] CreateViewCommand command,
        CancellationToken cancellationToken)
    {
        var viewId = await _mediator.Send(command, cancellationToken);
        return Ok(new { Id = viewId });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateView(
        Guid id,
        [FromBody] UpdateViewCommand command,
        CancellationToken cancellationToken)
    {
        if (id != command.Id)
            return BadRequest("Route id does not match body id.");

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteView(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteViewCommand(id), cancellationToken);
        return NoContent();
    }
}
