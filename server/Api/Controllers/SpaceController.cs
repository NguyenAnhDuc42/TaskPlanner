using Microsoft.AspNetCore.Mvc;
namespace Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class SpacesController : ControllerBase
    {
        private readonly IHandler _handler;

        public SpacesController(IHandler iHandler)
        {
            _handler = iHandler;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSpaceCommand command, CancellationToken cancellationToken)
        {
            var result = await _handler.SendAsync<CreateSpaceCommand, Guid>(command, cancellationToken);
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
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSpaceRequest request, CancellationToken cancellationToken)
        {
            var command = new UpdateSpaceCommand(
                SpaceId: id,
                Name: request.Name,
                Color: request.Color,
                Icon: request.Icon,
                IsPrivate: request.IsPrivate
            );

            var result = await _handler.SendAsync(command, cancellationToken);
            return result.ToActionResult();
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            var command = new DeleteSpaceCommand(id);
            var result = await _handler.SendAsync(command, cancellationToken);
            return result.ToActionResult();
        }

        [HttpGet("{id:guid}/items")]
        public async Task<IActionResult> GetItems(Guid id, CancellationToken cancellationToken)
        {
            var result = await _handler.QueryAsync<GetSpaceItemsQuery, TaskViewData>(new GetSpaceItemsQuery(id), cancellationToken);
            return result.ToActionResult();
        }

        [HttpPost("batch-update")]
        public async Task<IActionResult> BatchUpdate([FromBody] BatchUpdateSpaceItemsCommand command, CancellationToken cancellationToken)
        {
            var result = await _handler.SendAsync(command, cancellationToken);
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
        public async Task<IActionResult> UpdateAccess(Guid id, [FromBody] System.Collections.Generic.IEnumerable<EntityAccessRowsValue> rows, CancellationToken cancellationToken)
        {
            var command = new EntityAccessBatchCommand(id, rows);
            var result = await _handler.SendAsync(command, cancellationToken);
            return result.ToActionResult();
        }
    }

    public record UpdateSpaceRequest(
        string? Name,
        string? Color,
        string? Icon,
        bool? IsPrivate
    );
}


