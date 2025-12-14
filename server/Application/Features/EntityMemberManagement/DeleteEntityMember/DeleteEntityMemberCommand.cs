using Application.Common.Interfaces;
using Domain.Enums.RelationShip;
using MediatR;

namespace Application.Features.EntityMemberManagement.DeleteEntityMember;

public record DeleteEntityMemberCommand(
    Guid LayerId,
    EntityLayerType LayerType,
    List<Guid> UserIds
) : ICommand<Unit>;
