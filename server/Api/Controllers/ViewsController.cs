using Application.Features.ViewFeatures;
using Domain.Enums.RelationShip;
using Microsoft.AspNetCore.Mvc;
using Application.Common.Interfaces;
using Api.Extensions;

namespace Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ViewsController : ControllerBase
{
    private readonly IHandler _handler;

    public ViewsController(IHandler handler)
    {
        _handler = handler;
    }

    [HttpGet]
    public async Task<IActionResult> GetViews(
        [FromQuery] Guid layerId,
        [FromQuery] EntityLayerType layerType,
        CancellationToken ct)
    {
        var result = await _handler.QueryAsync<GetViewsQuery, List<ViewDto>>(new GetViewsQuery(layerId, layerType), ct);
        return result.ToActionResult();
    }

    [HttpPost]
    public async Task<IActionResult> CreateView([FromBody] CreateViewCommand command, CancellationToken ct)
    {
        var result = await _handler.SendAsync<CreateViewCommand, Guid>(command, ct);
        return result.ToActionResult();
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateView(Guid id, [FromBody] UpdateViewCommand command, CancellationToken ct)
    {
        if (id != command.Id) return BadRequest("Route id does not match body id.");
        var result = await _handler.SendAsync(command, ct);
        return result.ToActionResult();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteView(Guid id, CancellationToken ct)
    {
        var result = await _handler.SendAsync(new DeleteViewCommand(id), ct);
        return result.ToActionResult();
    }

    [HttpGet("{id:guid}/data")]
    public async Task<IActionResult> GetData(Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 100, CancellationToken ct = default)
    {
        var result = await _handler.QueryAsync<GetViewDataQuery, ViewDataResponse>(new GetViewDataQuery(id, page, pageSize), ct);
        return result.ToActionResult();
    }
}
