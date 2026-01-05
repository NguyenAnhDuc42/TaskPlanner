using Application.Common.Filters;
using Application.Common.Results;
using Application.Contract.WorkspaceContract;
using Application.Features.WorkspaceFeatures.SelfManagement.CreateWorkspace;
using Application.Features.WorkspaceFeatures.UpdateWorkspace;
using Application.Features.WorkspaceFeatures.GetWorkspaceList;
using Domain.Enums.Workspace;
using MediatR;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WorkspaceController : ControllerBase
    {
        private readonly IMediator _mediator;

        public WorkspaceController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> CreateWorkspace([FromBody] CreateWorkspaceCommand command, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result); // or return CreatedAtAction if appropriate
        }

        // JSON Patch method stays mostly the same
        [HttpPatch("{id:guid}")]
        public async Task<IActionResult> UpdateWorkspace(Guid id, [FromBody] JsonPatchDocument<UpdateWorkspaceCommand> patch, CancellationToken cancellationToken)
        {
            if (patch == null || patch.Operations.Count == 0)
                return BadRequest("A valid JSON Patch document is required.");

            var command = new UpdateWorkspaceCommand { Id = id };
            patch.ApplyTo(command, error =>
            {
                var message = error?.ErrorMessage ?? "Invalid patch operation.";
                ModelState.AddModelError(string.Empty, message);
            });

            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            await _mediator.Send(command, cancellationToken);
            return NoContent();
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<WorkspaceSummaryDto>>> GetWorkspaces(
            [FromQuery] string? cursor,
            [FromQuery] string? name,
            [FromQuery] int pageSize = 10,
            [FromQuery] bool owned = false,
            [FromQuery] bool isArchived = false,
            [FromQuery] WorkspaceVariant? variant = null,
            CancellationToken cancellationToken = default)
        {
            var pagination = new CursorPaginationRequest(cursor, pageSize);
            var filter = new WorkspaceFilter(name, owned, isArchived, variant);
            var query = new GetWorksapceListQuery(pagination, filter);

            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
    }

}