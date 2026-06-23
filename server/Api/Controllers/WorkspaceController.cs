using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
namespace Api
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
            var result = await _handler.SendAsync<CreateWorkspaceCommand, WorkspaceSnippetRecord>(command, cancellationToken);
            return result.ToActionResult();
        }

        [HttpPatch("{id:guid}")]
        public async Task<IActionResult> UpdateWorkspace(Guid id, [FromBody] UpdateWorkspaceCommand command, CancellationToken cancellationToken)
        {
            var result = await _handler.SendAsync(command with { Id = id }, cancellationToken);
            return result.ToActionResult();
        }

        [HttpPut("{id:guid}/pin")]
        public async Task<IActionResult> SetWorkspacePin(
            Guid id,
            [FromBody] SetWorkspacePinCommand command,
            CancellationToken cancellationToken)
        {
            var result = await _handler.SendAsync<SetWorkspacePinCommand, bool>(command with { WorkspaceId = id }, cancellationToken);
            return result.ToActionResult();
        }

        [HttpPost("join")]
        public async Task<IActionResult> JoinWorkspaceByCode(
            [FromBody] JoinWorkspaceByCodeCommand command,
            CancellationToken cancellationToken)
        {
            var result = await _handler.SendAsync<JoinWorkspaceByCodeCommand, JoinWorkspaceByCodeResult>(command, cancellationToken);
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

            var result = await _handler.QueryAsync<GetWorksapceListQuery, PagedResult<WorkspaceSnippetRecord>>(query, cancellationToken);
            return result.ToActionResult();
        }

        [HttpGet("{id:guid}/nodes/spaces")]
        public async Task<IActionResult> GetNodeSpaces(
            Guid id, 
            [FromQuery] CursorPaginationRequest pagination,
            CancellationToken cancellationToken = default)
        {
            var result = await _handler.QueryAsync<GetNodeSpacesQuery, PagedResult<SpaceRecord>>(new GetNodeSpacesQuery(id, pagination), cancellationToken);
            return result.ToActionResult();
        }

        [HttpGet("{id:guid}/nodes/{nodeId:guid}/tasks")]
        public async Task<IActionResult> GetNodeTasks(
            Guid id,
            Guid nodeId,
            [FromQuery] string parentType,
            [FromQuery] CursorPaginationRequest pagination,
            CancellationToken cancellationToken = default)
        {
            var result = await _handler.QueryAsync<GetNodeTasksQuery, PagedResult<TaskRecord>>(new GetNodeTasksQuery(id, nodeId, parentType, pagination), cancellationToken);
            return result.ToActionResult();
        }

        [HttpGet("{id:guid}/nodes/{nodeId:guid}/folders")]
        public async Task<IActionResult> GetNodeFolders(
            Guid id, 
            Guid nodeId, 
            [FromQuery] CursorPaginationRequest pagination,
            CancellationToken cancellationToken = default)
        {
            var result = await _handler.QueryAsync<GetNodeFoldersQuery, PagedResult<FolderRecord>>(new GetNodeFoldersQuery(id, nodeId, pagination), cancellationToken);
            return result.ToActionResult();
        }

        [HttpGet("{id:guid}/me/permissions")]
        public async Task<IActionResult> GetMyWorkspacePermissions(
            Guid id,
            CancellationToken cancellationToken)
        {
            var result = await _handler.QueryAsync<GetDetailWorkspaceQuery, WorkspaceRecord>(new GetDetailWorkspaceQuery(id), cancellationToken);
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

            var result = await _handler.QueryAsync<GetMembersQuery, PagedResult<MemberRecord>>(query, cancellationToken);
            return result.ToActionResult();
        }

        [HttpPost("{id:guid}/members")]
        public async Task<IActionResult> AddMembers(
            Guid id,
            [FromBody] AddMembersCommand command,
            CancellationToken cancellationToken)
        {
            var result = await _handler.SendAsync(command with { WorkspaceId = id }, cancellationToken);
            return result.ToActionResult();
        }

        [HttpPatch("{id:guid}/members")]
        public async Task<IActionResult> UpdateMembers(
            Guid id,
            [FromBody] UpdateMembersCommand command,
            CancellationToken cancellationToken)
        {
            var result = await _handler.SendAsync(command with { WorkspaceId = id }, cancellationToken);
            return result.ToActionResult();
        }

        [HttpDelete("{id:guid}/members")]
        public async Task<IActionResult> RemoveMembers(
            Guid id,
            [FromBody] RemoveMembersCommand command,
            CancellationToken cancellationToken)
        {
            var result = await _handler.SendAsync(command with { WorkspaceId = id }, cancellationToken);
            return result.ToActionResult();
        }

        [HttpPost("{id:guid}/nodes/move")]
        public async Task<IActionResult> MoveHierarchyItem(
            Guid id,
            [FromBody] MoveItemCommand command,
            CancellationToken cancellationToken)
        {
            var result = await _handler.SendAsync(command, cancellationToken);
            return result.ToActionResult();
        }

        [HttpPost("{id:guid}/nodes/batch-move")]
        public async Task<IActionResult> BatchMoveHierarchyItems(
            Guid id,
            [FromBody] BatchMoveItemCommand command,
            CancellationToken cancellationToken)
        {
            var result = await _handler.SendAsync(command, cancellationToken);
            return result.ToActionResult();
        }

        [HttpGet("{id:guid}/favorites")]
        public async Task<IActionResult> GetFavorites(
            Guid id,
            [FromQuery] CursorPaginationRequest pagination,
            CancellationToken cancellationToken)
        {
            var result = await _handler.QueryAsync<GetFavoritesQuery, GetFavoritesResponse>(
                new GetFavoritesQuery(pagination), cancellationToken);
            return result.ToActionResult();
        }

        [HttpPut("{id:guid}/favorites/reorder")]
        public async Task<IActionResult> ReorderFavorite(
            Guid id,
            [FromBody] ReorderFavoriteCommand command,
            CancellationToken cancellationToken)
        {
            var result = await _handler.SendAsync(command, cancellationToken);
            return result.ToActionResult();
        }

        [HttpPost("{id:guid}/favorites/toggle")]
        public async Task<IActionResult> ToggleFavorite(
            Guid id,
            [FromBody] ToggleFavoriteCommand command,
            CancellationToken cancellationToken)
        {
            var result = await _handler.SendAsync<ToggleFavoriteCommand, ToggleFavoriteResponse>(command, cancellationToken);
            return result.ToActionResult();
        }

    }


}



