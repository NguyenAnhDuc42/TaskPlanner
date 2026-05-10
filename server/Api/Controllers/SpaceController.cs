using Application.Features.SpaceFeatures;
using Domain.Enums.RelationShip;
using Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Api.Extensions;

namespace Api.Controllers
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
            var result = await _handler.QueryAsync<GetSpaceDetailQuery, SpaceDetailDto>(query, cancellationToken);
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
            var result = await _handler.QueryAsync<GetSpaceItemsQuery, Application.Features.ViewFeatures.TaskViewData>(new GetSpaceItemsQuery(id), cancellationToken);
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
