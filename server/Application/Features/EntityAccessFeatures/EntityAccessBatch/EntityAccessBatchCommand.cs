
using Application.Common.Interfaces;
using Domain.Enums;
using Domain.Enums.RelationShip;

namespace Application.Features.EntityAccessFeatures;

public record EntityAccessBatchCommand(Guid SpaceId, IEnumerable<EntityAccessRowsValue> Rows) : ICommandRequest, IAuthorizedWorkspaceRequest;

public record EntityAccessRowsValue(Guid MemberId, AccessLevel AccessLevel,RowAction Action);

