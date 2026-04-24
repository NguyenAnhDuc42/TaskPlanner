using Application.Common.Filters;
using Application.Common.Results;
using Application.Features.WorkspaceFeatures;
using Microsoft.AspNetCore.Mvc;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Api.Extensions;
using Application.Common.Interfaces;

namespace Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class WorkspacesController : ControllerBase
    {
        private readonly IHandler _handler;

        public WorkspacesController(IHandler iHandler)
        {
            _handler = iHandler;
        }

        [HttpPost]
        public async Task<IActionResult> CreateWorkspace([FromBody] CreateWorkspaceCommand command, CancellationToken cancellationToken)
        {
            var result = await _handler.SendAsync<CreateWorkspaceCommand, Guid>(command, cancellationToken);
            return result.ToActionResult();
        }

        [HttpPatch("{id:guid}")]
        public async Task<IActionResult> UpdateWorkspace(Guid id, [FromBody] UpdateWorkspaceRequest request, CancellationToken cancellationToken)
        {
            var command = new UpdateWorkspaceCommand(
                Id: id,
                Name: request.Name,
                Description: request.Description,
                Color: request.Color,
                Icon: request.Icon,
                Theme: request.Theme,
                StrictJoin: request.StrictJoin,
                IsArchived: request.IsArchived,
                RegenerateJoinCode: request.RegenerateJoinCode ?? false
            );

            var result = await _handler.SendAsync(command, cancellationToken);
            return result.ToActionResult();
        }

        [HttpPut("{id:guid}/pin")]
        public async Task<IActionResult> SetWorkspacePin(
            Guid id,
            [FromBody] SetWorkspacePinRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _handler.SendAsync(new SetWorkspacePinCommand(id, request.IsPinned), cancellationToken);
            return result.ToActionResult();
        }

        [HttpPost("join")]
        public async Task<IActionResult> JoinWorkspaceByCode(
            [FromBody] JoinWorkspaceByCodeRequest request,
            CancellationToken cancellationToken)
        {
            var result = await _handler.SendAsync<JoinWorkspaceByCodeCommand, JoinWorkspaceByCodeResult>(new JoinWorkspaceByCodeCommand(request.JoinCode), cancellationToken);
            return result.ToActionResult();
        }

        [HttpGet]
        public async Task<IActionResult> GetWorkspaces(
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

            var result = await _handler.QueryAsync<GetWorksapceListQuery, PagedResult<WorkspaceSummaryDto>>(query, cancellationToken);
            return result.ToActionResult();
        }

        [HttpGet("{id:guid}/hierarchy")]
        public async Task<IActionResult> GetHierarchy(Guid id, CancellationToken cancellationToken)
        {
            var query = new GetHierarchyQuery(id);
            var result = await _handler.QueryAsync<GetHierarchyQuery, WorkspaceHierarchyDto>(query, cancellationToken);
            return result.ToActionResult();
        }

        [HttpGet("{id:guid}/hierarchy/nodes/{nodeId}/tasks")]
        public async Task<IActionResult> GetNodeTasks(
            Guid id,
            Guid nodeId,
            [FromQuery] string parentType,
            [FromQuery] string? cursorOrderKey,
            [FromQuery] string? cursorTaskId,
            [FromQuery] int pageSize = 50,
            CancellationToken cancellationToken = default)
        {
            var query = new GetNodeTasksQuery(id, nodeId, parentType, cursorOrderKey, cursorTaskId, pageSize);
            var result = await _handler.QueryAsync<GetNodeTasksQuery, NodeTasksDto>(query, cancellationToken);
            return result.ToActionResult();
        }

        [HttpGet("{id:guid}/hierarchy/nodes/{nodeId:guid}/folders")]
        public async Task<IActionResult> GetNodeFolders(Guid id, Guid nodeId, CancellationToken cancellationToken)
        {
            var query = new GetNodeFoldersQuery(id, nodeId);
            var result = await _handler.QueryAsync<GetNodeFoldersQuery, List<FolderHierarchyDto>>(query, cancellationToken);
            return result.ToActionResult();
        }

        [HttpGet("{id:guid}/me/permissions")]
        public async Task<IActionResult> GetMyWorkspacePermissions(
            Guid id,
            CancellationToken cancellationToken)
        {
            var result = await _handler.QueryAsync<GetDetailWorkspaceQuery, WorkspaceSecurityContextDto>(new GetDetailWorkspaceQuery(id), cancellationToken);
            return result.ToActionResult();
        }

        [HttpGet("{id:guid}/members")]
        public async Task<IActionResult> GetMembers(
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

            var result = await _handler.QueryAsync<GetMembersQuery, PagedResult<MemberDto>>(query, cancellationToken);
            return result.ToActionResult();
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
            var result = await _handler.SendAsync(command, cancellationToken);
            return result.ToActionResult();
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
            var result = await _handler.SendAsync<UpdateMembersCommand, Guid>(command, cancellationToken);
            return result.ToActionResult();
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
            var result = await _handler.SendAsync<RemoveMembersCommand, Guid>(command, cancellationToken);
            return result.ToActionResult();
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

            var result = await _handler.SendAsync(command, cancellationToken);
            return result.ToActionResult();
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
    public record UpdateWorkspaceRequest(
        string? Name,
        string? Description,
        string? Color,
        string? Icon,
        Theme? Theme,
        bool? StrictJoin,
        bool? IsArchived,
        bool? RegenerateJoinCode
    );
}
