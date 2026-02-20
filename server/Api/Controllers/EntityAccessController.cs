using Application.Features.EntityAccessManagement.UpdateEntityAccessBulk;
using Application.Features.EntityAccessManagement.GetEntityAccessList;
using Domain.Enums.RelationShip;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class EntityAccessController : ControllerBase
{
    private readonly IMediator _mediator;

    public EntityAccessController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("bulk")]
    public async Task<IActionResult> UpdateBulk([FromBody] UpdateEntityAccessBulkCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{entityId:guid}/{layerType}")]
    public async Task<IActionResult> GetAccessList(Guid entityId, EntityLayerType layerType, CancellationToken cancellationToken)
    {
        var query = new GetEntityAccessListQuery(entityId, layerType);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }
}
