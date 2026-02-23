using Application.Common.Interfaces;
using Domain.Enums.RelationShip;

namespace Application.Features.ViewFeatures.GetViewData;

public record GetViewDataQuery(Guid ViewId) : IQuery<object>;
