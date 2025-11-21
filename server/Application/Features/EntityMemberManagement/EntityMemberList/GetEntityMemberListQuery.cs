using Application.Common.Filters;
using Application.Common.Interfaces;
using Application.Common.Results;
using Domain.Enums.RelationShip;

namespace Application.Features.EntityMemberManagement.EntityMemberList;

public record GetEntityMemberListQuery(
    Guid LayerId,
    EntityLayerType LayerType,
    CursorPaginationRequest Pagination
) : IQuery<PagedResult<EntityMemberDto>>;
