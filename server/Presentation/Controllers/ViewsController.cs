using Application.Features.ViewFeatures.CreateView;
using Application.Features.ViewFeatures.DeleteView;
using Application.Features.ViewFeatures.GetViewData;
using Application.Features.ViewFeatures.GetViews;
using Application.Features.ViewFeatures.UpdateView;
using Microsoft.AspNetCore.Mvc;
using server.Presentation.Controllers;

namespace server.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ViewsController : BaseController
{
    [HttpGet("{id}/data")]
    public async Task<IActionResult> GetViewData(Guid id)
    {
        var result = await Mediator.Send(new GetViewDataQuery(id));
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetViews([FromQuery] Guid layerId, [FromQuery] Domain.Enums.RelationShip.EntityLayerType layerType)
    {
        var result = await Mediator.Send(new GetViewsQuery(layerId, layerType));
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateViewCommand command)
    {
        var result = await Mediator.Send(command);
        return Ok(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, UpdateViewCommand command)
    {
        if (id != command.Id) return BadRequest("ID mismatch");
        await Mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await Mediator.Send(new DeleteViewCommand(id));
        return NoContent();
    }
}
