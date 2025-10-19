using Application.Common.Filters;
using Application.Common.Results;
using Application.Contract.WorkspaceContract;
using Application.Features.WorkspaceFeatures.CreateWrokspace;
using Application.Features.WorkspaceFeatures.SelfMange.GetWorkspaceList;
using Application.Features.WorkspaceFeatures.SelfMange.UpdateWorkspace;
using Domain.Enums.Workspace;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WorkspaceController : BaseController
    {
        protected WorkspaceController(IMediator mediator) : base(mediator) { }

        [HttpPost]
        public async Task<IActionResult> CreateWorkspace([FromBody] CreateWorkspaceCommand command, CancellationToken cancellationToken)
        {
            return await SendRequest(command, cancellationToken);
        }

        [HttpPatch("{id:guid}")]
        public async Task<IActionResult> UpdateWorkspace(Guid id, [FromBody] JsonPatchDocument<UpdateWorkspaceCommand> patch, CancellationToken cancellationToken)
        {
            if (patch == null || patch.Operations.Count == 0)
            {
                return BadRequest("A valid JSON Patch document is required.");
            }

            var command = new UpdateWorkspaceCommand { Id = id };
            patch.ApplyTo(command, error =>
            {
                // Manually add each error to the ModelState. Use Path as the key when available.
                var message = error?.ErrorMessage ?? "Invalid patch operation.";
                ModelState.AddModelError(string.Empty, message);
            });

            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }
            await _mediator.Send(command, cancellationToken);
            return NoContent();
        }
        [HttpGet]
        public async Task<ActionResult<PagedResult<WorkspaceSummary>>> GetWorkspaces(
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
