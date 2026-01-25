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
using Domain.Enums;
using Application.Contract.UserContract;
using Application.Features.WorkspaceFeatures.MemberManage.GetMembers;
using Application.Features.WorkspaceFeatures.MemberManage.AddMembers;
using Application.Features.WorkspaceFeatures.MemberManage.UpdateMembers;
using Application.Features.WorkspaceFeatures.MemberManage.RemoveMembers;

namespace Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WorkspacesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public WorkspacesController(IMediator mediator)
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
            [FromQuery] bool? owned = null,
            [FromQuery] bool? isArchived = null,
            [FromQuery] WorkspaceVariant? variant = null,
            [FromQuery] SortDirection direction = SortDirection.Ascending,
            CancellationToken cancellationToken = default)
        {
            var pagination = new CursorPaginationRequest(cursor, pageSize, Direction: direction);
            var filter = new WorkspaceFilter(name, owned, isArchived, variant);
            var query = new GetWorksapceListQuery(pagination, filter);

            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        [HttpGet("{id:guid}/members")]
        public async Task<ActionResult<PagedResult<MemberDto>>> GetMembers(
            Guid id,
            [FromQuery] string? cursor,
            [FromQuery] string? name,
            [FromQuery] string? email,
            [FromQuery] Guid? spaceId,
            [FromQuery] Guid? taskId,
            [FromQuery] Role? role,
            [FromQuery] int pageSize = 10,
            CancellationToken cancellationToken = default)
        {
            var pagination = new CursorPaginationRequest(cursor, pageSize);
            var filter = new GetMembersFilter(name, email, spaceId, taskId, role);
            var query = new GetMembersQuery(pagination,id,filter);

            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
        [HttpPost("{id:guid}/members")]
        public async Task<IActionResult> AddMembers(
            Guid id,
            [FromBody] AddMembersRequest request,
            CancellationToken cancellationToken)
        {
            var command = new AddMembersCommand(
                workspaceId: id,
                members: request.Members,
                enableEmail: request.EnableEmail,
                message: request.Message
            );
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }

        [HttpPatch("{id:guid}/members")]
        public async Task<IActionResult> UpdateMembers(
            Guid id,
            [FromBody] UpdateMembersRequest request,
            CancellationToken cancellationToken)
        {
            var command = new UpdateMembersCommand(
                workspaceId: id,
                members: request.Members
            );
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }

        [HttpDelete("{id:guid}/members")]
        public async Task<IActionResult> RemoveMembers(
            Guid id,
            [FromBody] RemoveMembersRequest request,
            CancellationToken cancellationToken)
        {
            var command = new Application.Features.WorkspaceFeatures.MemberManage.RemoveMembers.RemoveMembersCommand(
                workspaceId: id,
                memberIds: request.MemberIds
            );
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
    }

    public record AddMembersRequest(List<MemberValue> Members, bool? EnableEmail = false, string? Message = null);
    public record UpdateMembersRequest(List<UpdateMemberValue> Members);
    public record RemoveMembersRequest(List<Guid> MemberIds);


}