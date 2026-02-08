using Application.Features.FolderFeatures.SelfManagement.CreateFolder;
using Application.Features.FolderFeatures.SelfManagement.DeleteFolder;
using Application.Features.FolderFeatures.SelfManagement.UpdateFolder;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    // Note: Creating a folder is typically done under a space, so the route might be best placed on SpaceController.
    // However, for clean separation, we can have a dedicated controller.
    // We can also route it like `api/folders` and expect SpaceId in the body.
    // But RESTful convention often uses nested paths.
    // Given the command structure, let's keep it simple.
    
    public class FoldersController : ControllerBase
    {
        private readonly IMediator _mediator;

        public FoldersController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("space/{spaceId}")]
        public async Task<IActionResult> Create(Guid spaceId, [FromBody] CreateFolderCommand command, CancellationToken cancellationToken)
        {
             if (spaceId != command.spaceId)
            {
                 return BadRequest("Space ID mismatch");
            }
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateFolderCommand command, CancellationToken cancellationToken)
        {
            if (id != command.FolderId)
            {
                return BadRequest("ID mismatch");
            }

            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            var command = new DeleteFolderCommand(id);
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
    }
}
