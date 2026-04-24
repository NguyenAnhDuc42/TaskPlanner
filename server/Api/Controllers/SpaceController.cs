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

        [HttpPost("{workspaceId:guid}")]
        public async Task<IActionResult> Create(Guid workspaceId, [FromBody] CreateSpaceCommand command, CancellationToken cancellationToken)
        {
            // Ensure workspaceId is correctly set if command has it
            var result = await _handler.SendAsync<CreateSpaceCommand, Guid>(command, cancellationToken);
            return result.ToActionResult();
        }

        [HttpPut("{workspaceId:guid}/{id:guid}")]
        public async Task<IActionResult> Update(Guid workspaceId, Guid id, [FromBody] UpdateSpaceRequest request, CancellationToken cancellationToken)
        {
            var command = new UpdateSpaceCommand(
                workspaceId: workspaceId,
                SpaceId: id,
                Name: request.Name,
                Description: request.Description,
                Color: request.Color,
                Icon: request.Icon,
                IsPrivate: request.IsPrivate
            );

            var result = await _handler.SendAsync(command, cancellationToken);
            return result.ToActionResult();
        }

        [HttpDelete("{workspaceId:guid}/{id:guid}")]
        public async Task<IActionResult> Delete(Guid workspaceId, Guid id, CancellationToken cancellationToken)
        {
            var command = new DeleteSpaceCommand(workspaceId, id);
            var result = await _handler.SendAsync(command, cancellationToken);
            return result.ToActionResult();
        }
    }

    public record UpdateSpaceRequest(
        string? Name,
        string? Description,
        string? Color,
        string? Icon,
        bool? IsPrivate
    );
}
