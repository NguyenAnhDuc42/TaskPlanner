using Application.Common.Interfaces;
using Domain.Enums.RelationShip;
using MediatR;

namespace Application.Features.EntityMemberManagement.CreateEntityMember;

public record CreateEntityMemberCommand(
    Guid LayerId,
    EntityLayerType LayerType,
    List<Guid> UserIds,
    AccessLevel AccessLevel
) : ICommand<Unit>;
