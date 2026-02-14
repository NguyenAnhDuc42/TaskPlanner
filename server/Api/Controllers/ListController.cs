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
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateListRequest request, CancellationToken cancellationToken)
        {
            var membersToAddOrUpdate =
                request.MembersToAddOrUpdate
                ?? request.MemberIdsToAdd?.Select(memberId => new UpdateListMemberValue(memberId, null)).ToList();

            var command = new UpdateListCommand(
                ListId: id,
                Name: request.Name,
                Color: request.Color,
                Icon: request.Icon,
                IsPrivate: request.IsPrivate,
                StartDate: request.StartDate,
                DueDate: request.DueDate,
                MembersToAddOrUpdate: membersToAddOrUpdate
            );

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

    public record UpdateListRequest(
        string? Name,
        string? Color,
        string? Icon,
        bool? IsPrivate,
        DateTimeOffset? StartDate,
        DateTimeOffset? DueDate,
        List<Guid>? MemberIdsToAdd,
        List<UpdateListMemberValue>? MembersToAddOrUpdate
    );
}
