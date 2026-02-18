using Application.Features.SpaceFeatures.SelfManagement.CreateSpace;
using Application.Features.SpaceFeatures.SelfManagement.DeleteSpace;
using Application.Features.SpaceFeatures.SelfManagement.UpdateSpace;
using Application.Features.EntityAccessManagement.GetEntityAccessList;
using Domain.Enums.RelationShip;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SpacesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public SpacesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSpaceCommand command, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSpaceRequest request, CancellationToken cancellationToken)
        {
            var membersToAddOrUpdate =
                request.MembersToAddOrUpdate
                ?? request.MemberIdsToAdd?.Select(memberId => new UpdateSpaceMemberValue(memberId, null)).ToList();

            var command = new UpdateSpaceCommand(
                SpaceId: id,
                Name: request.Name,
                Description: request.Description,
                Color: request.Color,
                Icon: request.Icon,
                IsPrivate: request.IsPrivate,
                MembersToAddOrUpdate: membersToAddOrUpdate
            );

            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        {
            var command = new DeleteSpaceCommand(id);
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }

        [HttpGet("{id:guid}/members-access")]
        public async Task<IActionResult> GetMembersAccess(Guid id, CancellationToken cancellationToken)
        {
            var query = new GetEntityAccessListQuery(id, EntityLayerType.ProjectSpace);
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
    }

    public record UpdateSpaceRequest(
        string? Name,
        string? Description,
        string? Color,
        string? Icon,
        bool? IsPrivate,
        List<Guid>? MemberIdsToAdd,
        List<UpdateSpaceMemberValue>? MembersToAddOrUpdate
    );
}
