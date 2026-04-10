using Application.Common.Filters;
using Application.Common.Results;
using Application.Features.WorkspaceFeatures.SelfManagement.CreateWorkspace;
using Application.Features.WorkspaceFeatures.SelfManagement.GetWorkspaceList;
using Domain.Enums.Workspace;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Application.Features.WorkspaceFeatures.MemberManage.GetMembers;
using Application.Features.WorkspaceFeatures.MemberManage.AddMembers;
using Application.Features.WorkspaceFeatures.HierarchyManagement.GetHierarchy;
using Application.Features.WorkspaceFeatures.MemberManage.UpdateMembers;
using Application.Features.WorkspaceFeatures.SelfManagement.GetDetailWorkspace;
using Application.Features.WorkspaceFeatures.SelfManagement.SetWorkspacePin;
using Application.Features.WorkspaceFeatures.SelfManagement.JoinWorkspaceByCode;
using Application.Features.WorkspaceFeatures.UpdateWorkspace;
using Application.Features;
using Application.Features.WorkspaceFeatures.MemberManage.RemoveMembers;
using Application.Features.WorkspaceFeatures.HierarchyManagement.MoveItem;

namespace Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class WorkspacesController : ControllerBase
    {
        private readonly ICommandHandler<CreateWorkspaceCommand, Guid> _createHandler;
        private readonly ICommandHandler<UpdateWorkspaceCommand> _updateHandler;
        private readonly ICommandHandler<SetWorkspacePinCommand> _setPinHandler;
        private readonly ICommandHandler<JoinWorkspaceByCodeCommand, Guid> _joinHandler;
        private readonly IQueryHandler<GetWorksapceListQuery, PagedResult<WorkspaceSummaryDto>> _getListHandler;
        private readonly IQueryHandler<GetHierarchyQuery, WorkspaceHierarchyDto> _getHierarchyHandler;
        private readonly IQueryHandler<GetNodeTasksQuery, NodeTasksDto> _getNodeTasksHandler;
        private readonly IQueryHandler<GetDetailWorkspaceQuery, WorkspaceSecurityContextDto> _getDetailHandler;
        private readonly IQueryHandler<GetMembersQuery, PagedResult<MemberDto>> _getMembersHandler;
        private readonly ICommandHandler<AddMembersCommand, Guid> _addHandler;
        private readonly ICommandHandler<UpdateMembersCommand> _updateMembersHandler;
        private readonly ICommandHandler<RemoveMembersCommand> _removeHandler;
        private readonly ICommandHandler<MoveItemCommand> _moveHandler;

        public WorkspacesController(
            ICommandHandler<CreateWorkspaceCommand, Guid> createHandler,
            ICommandHandler<UpdateWorkspaceCommand> updateHandler,
            ICommandHandler<SetWorkspacePinCommand> setPinHandler,
            ICommandHandler<JoinWorkspaceByCodeCommand, Guid> joinHandler,
            IQueryHandler<GetWorksapceListQuery, PagedResult<WorkspaceSummaryDto>> getListHandler,
            IQueryHandler<GetHierarchyQuery, WorkspaceHierarchyDto> getHierarchyHandler,
            IQueryHandler<GetNodeTasksQuery, NodeTasksDto> getNodeTasksHandler,
            IQueryHandler<GetDetailWorkspaceQuery, WorkspaceSecurityContextDto> getDetailHandler,
            IQueryHandler<GetMembersQuery, PagedResult<MemberDto>> getMembersHandler,
            ICommandHandler<AddMembersCommand, Guid> addHandler,
            ICommandHandler<UpdateMembersCommand> updateMembersHandler,
            ICommandHandler<RemoveMembersCommand> removeHandler,
            ICommandHandler<MoveItemCommand> moveHandler)
        {
            _createHandler = createHandler;
            _updateHandler = updateHandler;
            _setPinHandler = setPinHandler;
            _joinHandler = joinHandler;
            _getListHandler = getListHandler;
            _getHierarchyHandler = getHierarchyHandler;
            _getNodeTasksHandler = getNodeTasksHandler;
            _getDetailHandler = getDetailHandler;
            _getMembersHandler = getMembersHandler;
            _addHandler = addHandler;
            _updateMembersHandler = updateMembersHandler;
            _removeHandler = removeHandler;
            _moveHandler = moveHandler;
        }

        [HttpPost]
        public async Task<IActionResult> CreateWorkspace([FromBody] CreateWorkspaceCommand command, CancellationToken cancellationToken)
        {
            var result = await _createHandler.Handle(command, cancellationToken);
            return Ok(result);
        }

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

            await _updateHandler.Handle(command, cancellationToken);
            return NoContent();
        }

        [HttpPut("{id:guid}/pin")]
        public async Task<IActionResult> SetWorkspacePin(
            Guid id,
            [FromBody] SetWorkspacePinRequest request,
            CancellationToken cancellationToken)
        {
            await _setPinHandler.Handle(new SetWorkspacePinCommand(id, request.IsPinned), cancellationToken);
            return NoContent();
        }

        [HttpPost("join")]
        public async Task<IActionResult> JoinWorkspaceByCode(
            [FromBody] JoinWorkspaceByCodeRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _joinHandler.Handle(new JoinWorkspaceByCodeCommand(request.JoinCode), cancellationToken);
            return Ok(result);
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<WorkspaceSummaryDto>>> GetWorkspaces(
            [FromQuery] string? cursor,
            [FromQuery] string? name,
            [FromQuery] int pageSize = 10,
            [FromQuery] bool? owned = null,
            [FromQuery] bool? isArchived = null,
            [FromQuery] SortDirection direction = SortDirection.Ascending,
            CancellationToken cancellationToken = default)
        {
            var pagination = new CursorPaginationRequest(cursor, pageSize, Direction: direction);
            var filter = new WorkspaceFilter(name, owned, isArchived);
            var query = new GetWorksapceListQuery(pagination, filter);

            var result = await _getListHandler.Handle(query, cancellationToken);
            return Ok(result);
        }

        [HttpGet("{id:guid}/hierarchy")]
        public async Task<ActionResult<WorkspaceHierarchyDto>> GetHierarchy(Guid id, CancellationToken cancellationToken)
        {
            var query = new GetHierarchyQuery(id);
            var result = await _getHierarchyHandler.Handle(query, cancellationToken);
            return Ok(result);
        }

        [HttpGet("{id:guid}/hierarchy/nodes/{nodeId}/tasks")]
        public async Task<ActionResult<NodeTasksDto>> GetNodeTasks(
            Guid id,
            Guid nodeId,
            [FromQuery] string parentType,
            [FromQuery] string? cursorOrderKey,
            [FromQuery] string? cursorTaskId,
            [FromQuery] int pageSize = 50,
            CancellationToken cancellationToken = default)
        {
            var query = new GetNodeTasksQuery(id, nodeId, parentType, cursorOrderKey, cursorTaskId, pageSize);
            var result = await _getNodeTasksHandler.Handle(query, cancellationToken);
            return Ok(result);
        }

        [HttpGet("{id:guid}/me/permissions")]
        public async Task<ActionResult<WorkspaceSecurityContextDto>> GetMyWorkspacePermissions(
            Guid id,
            CancellationToken cancellationToken)
        {
            var result = await _getDetailHandler.Handle(new GetDetailWorkspaceQuery(id), cancellationToken);
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
            var query = new GetMembersQuery(pagination, id, filter);

            var result = await _getMembersHandler.Handle(query, cancellationToken);
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
            var result = await _addHandler.Handle(command, cancellationToken);
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
            var result = await _updateMembersHandler.Handle(command, cancellationToken);
            return Ok(result);
        }

        [HttpDelete("{id:guid}/members")]
        public async Task<IActionResult> RemoveMembers(
            Guid id,
            [FromBody] RemoveMembersRequest request,
            CancellationToken cancellationToken)
        {
            var command = new RemoveMembersCommand(
                workspaceId: id,
                memberIds: request.MemberIds
            );
            var result = await _removeHandler.Handle(command, cancellationToken);
            return Ok(result);
        }

        [HttpPost("{id:guid}/hierarchy/move")]
        public async Task<IActionResult> MoveHierarchyItem(
            Guid id,
            [FromBody] MoveItemRequest request,
            CancellationToken cancellationToken)
        {
            var command = new MoveItemCommand(
                ItemId: request.ItemId,
                ItemType: request.ItemType,
                TargetParentId: request.TargetParentId,
                PreviousItemOrderKey: request.PreviousItemOrderKey,
                NextItemOrderKey: request.NextItemOrderKey,
                NewOrderKey: request.NewOrderKey
            );

            await _moveHandler.Handle(command, cancellationToken);
            return NoContent();
        }
    }

    public record MoveItemRequest(
        Guid ItemId,
        Domain.Enums.RelationShip.EntityLayerType ItemType,
        Guid? TargetParentId,
        string? PreviousItemOrderKey,
        string? NextItemOrderKey,
        string? NewOrderKey
    );

    public record AddMembersRequest(List<MemberValue> Members, bool? EnableEmail = false, string? Message = null);
    public record UpdateMembersRequest(List<UpdateMemberValue> Members);
    public record RemoveMembersRequest(List<Guid> MemberIds);
    public record SetWorkspacePinRequest(bool IsPinned);
    public record JoinWorkspaceByCodeRequest(string JoinCode);


}
