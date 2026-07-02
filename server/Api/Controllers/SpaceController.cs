using Microsoft.AspNetCore.Mvc;

namespace Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class SpacesController : ControllerBase
    {
        private readonly IHandler _handler;

        public SpacesController(IHandler iHandler)
        {
            _handler = iHandler;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Application.CreateSpaceCommand command, CancellationToken cancellationToken)
        {
            var result = await _handler.SendAsync<Application.CreateSpaceCommand, SpaceRecord>(command, cancellationToken);
            return result.ToActionResult();
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetDetail(Guid id, CancellationToken cancellationToken)
        {
            var query = new GetSpaceDetailQuery(id);
            var result = await _handler.QueryAsync<GetSpaceDetailQuery, SpaceRecord>(query, cancellationToken);
            return result.ToActionResult();
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] Application.UpdateSpaceCommand command, CancellationToken cancellationToken)
        {
            var result = await _handler.SendAsync(command with { SpaceId = id }, cancellationToken);
            return result.ToActionResult();
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            var command = new Application.DeleteSpaceCommand(id);
            var result = await _handler.SendAsync(command, cancellationToken);
            return result.ToActionResult();
        }

        [HttpGet("{id:guid}/items")]
        public async Task<IActionResult> GetItems(Guid id, [FromQuery] string? cursor, CancellationToken cancellationToken)
        {
            var result = await _handler.QueryAsync<GetSpaceItemsQuery, GetSpaceItemsResponse>(new GetSpaceItemsQuery(id, cursor), cancellationToken);
            return result.ToActionResult();
        }

        [HttpPut("{id:guid}/batch-update")]
        public async Task<IActionResult> BatchUpdate(Guid id, [FromBody] BatchUpdateSpaceItemsCommand command, CancellationToken cancellationToken)
        {
            var result = await _handler.SendAsync(command with { SpaceId = id }, cancellationToken);
            return result.ToActionResult();
        }

        [HttpGet("{id:guid}/access")]
        public async Task<IActionResult> GetAccess(Guid id, CancellationToken cancellationToken)
        {
            var query = new GetEntityAccessQuery(id);
            var result = await _handler.QueryAsync<GetEntityAccessQuery, IReadOnlyList<EntityAccessRecord>>(query, cancellationToken);
            return result.ToActionResult();
        }

        [HttpPost("{id:guid}/access")]
        public async Task<IActionResult> UpdateAccess(Guid id, [FromBody] System.Collections.Generic.IEnumerable<Application.EntityAccessRowsValue> rows, CancellationToken cancellationToken)
        {
            var command = new Application.EntityAccessBatchCommand(id, rows);
            var result = await _handler.SendAsync(command, cancellationToken);
            return result.ToActionResult();
        }

        [HttpGet("statuses")]
        public async Task<IActionResult> GetWorkspaceStatuses(CancellationToken cancellationToken)
        {
            var result = await _handler.QueryAsync<GetWorkspaceStatusesQuery, List<StatusRecord>>(new GetWorkspaceStatusesQuery(), cancellationToken);
            return result.ToActionResult();
        }

        [HttpPut("{id:guid}/statuses")]
        public async Task<IActionResult> UpdateStatuses(Guid id, [FromBody] List<Application.StatusUpdateValue> statuses, CancellationToken cancellationToken)
        {
            var result = await _handler.SendAsync(new Application.UpdateSpaceStatusesCommand(id, statuses), cancellationToken);
            return result.ToActionResult();
        }
    }


}


