using Application.Features.SpaceFeatures.SelfManagement.CreateSpace;
using Application.Features.SpaceFeatures.SelfManagement.DeleteSpace;
using Application.Features.SpaceFeatures.SelfManagement.UpdateSpace;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SpacesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public SpacesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSpaceCommand command, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSpaceCommand command, CancellationToken cancellationToken)
        {
            if (id != command.SpaceId)
            {
                return BadRequest("ID mismatch");
            }

            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            var command = new DeleteSpaceCommand(id);
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
    }
}
