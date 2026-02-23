using Application.Features.FolderFeatures.SelfManagement.CreateFolder;
using Application.Features.FolderFeatures.SelfManagement.DeleteFolder;
using Application.Features.FolderFeatures.SelfManagement.UpdateFolder;
using Application.Features.EntityAccessManagement.GetEntityAccessList;
using Domain.Enums.RelationShip;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]    
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
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateFolderRequest request, CancellationToken cancellationToken)
        {
            var command = new UpdateFolderCommand(
                FolderId: id,
                Name: request.Name,
                Color: request.Color,
                Icon: request.Icon,
                IsPrivate: request.IsPrivate,
                InheritStatus: request.InheritStatus
            );

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

        [HttpGet("{id:guid}/members-access")]
        public async Task<IActionResult> GetMembersAccess(Guid id, CancellationToken cancellationToken)
        {
            var query = new GetEntityAccessListQuery(id, EntityLayerType.ProjectFolder);
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
    }

    public record UpdateFolderRequest(
        string? Name,
        string? Color,
        string? Icon,
        bool? IsPrivate,
        bool? InheritStatus

    );
}
