using Application.Features.ListFeatures.SelfManagement.CreateList;
using Application.Features.ListFeatures.SelfManagement.DeleteList;
using Application.Features.ListFeatures.SelfManagement.UpdateList;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ListsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ListsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // Create List under Space
        [HttpPost("space/{spaceId}")]
        public async Task<IActionResult> CreateUnderSpace(Guid spaceId, [FromBody] CreateListCommand command, CancellationToken cancellationToken)
        {
             // Ensure the command targets the correct space and no folder
            if (spaceId != command.spaceId)
            {
                 return BadRequest("Space ID mismatch");
            }
             if (command.folderId.HasValue)
            {
                return BadRequest("For creating under space, Folder ID must be null");
            }

            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }

        // Create List under Folder
        [HttpPost("folder/{folderId}")]
         public async Task<IActionResult> CreateUnderFolder(Guid folderId, [FromBody] CreateListCommand command, CancellationToken cancellationToken)
        {
             // Ensure the command targets the correct folder
            if (folderId != command.folderId)
            {
                 return BadRequest("Folder ID mismatch");
            }

            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateListCommand command, CancellationToken cancellationToken)
        {
            if (id != command.ListId)
            {
                return BadRequest("ID mismatch");
            }

            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            var command = new DeleteListCommand(id);
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
    }
}
