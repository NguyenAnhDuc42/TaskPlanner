using Microsoft.AspNetCore.Mvc;
namespace Api;

[Route("api/[controller]")]
[ApiController]
public class ViewsController : ControllerBase
{
    private readonly IHandler _handler;

    public ViewsController(IHandler handler)
    {
        _handler = handler;
    }

    // [HttpGet]
    // public async Task<IActionResult> GetViews(
    //     [FromQuery] Guid layerId,
    //     [FromQuery] EntityLayerType layerType,
    //     CancellationToken cancellationToken)
    // {
    //     var result = await _handler.QueryAsync<GetViewsQuery, List<ViewDto>>(new GetViewsQuery(layerId, layerType), cancellationToken);
    //     return result.ToActionResult();
    // }

    // [HttpPost]
    // public async Task<IActionResult> CreateView([FromBody] CreateViewCommand command, CancellationToken cancellationToken)
    // {
    //     var result = await _handler.SendAsync<CreateViewCommand, Guid>(command, cancellationToken);
    //     return result.ToActionResult();
    // }

    // [HttpPut("{id:guid}")]
    // public async Task<IActionResult> UpdateView(Guid id, [FromBody] UpdateViewCommand command, CancellationToken cancellationToken)
    // {
    //     if (id != command.Id) return BadRequest("Route id does not match body id.");
    //     var result = await _handler.SendAsync(command, cancellationToken);
    //     return result.ToActionResult();
    // }

    // [HttpDelete("{id:guid}")]
    // public async Task<IActionResult> DeleteView(Guid id, CancellationToken cancellationToken)
    // {
    //     var result = await _handler.SendAsync(new DeleteViewCommand(id), cancellationToken);
    //     return result.ToActionResult();
    // }

    // [HttpGet("{id:guid}/data")]
    // public async Task<IActionResult> GetData(Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 100, CancellationToken cancellationToken = default)
    // {
    //     var result = await _handler.QueryAsync<GetViewDataQuery, ViewDataResponse>(new GetViewDataQuery(id, page, pageSize), cancellationToken);
    //     return result.ToActionResult();
    // }
}


