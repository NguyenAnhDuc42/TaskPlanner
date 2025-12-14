using Application.Common.Interfaces;
using Domain.Enums.RelationShip;
using MediatR;

namespace Application.Features.EntityMemberManagement.EditEntityMember;

public record EditEntityMemberCommand(
    Guid LayerId,
    EntityLayerType LayerType,
    List<Guid> UserIds,
    AccessLevel AccessLevel
) : ICommand<Unit>;
