using Application.Common.Interfaces;
using Domain.Enums.RelationShip;
using MediatR;
using System;

namespace Application.Features.DashboardFeatures.CreateDashboard;

[Obsolete("Dashboard features are legacy and will be removed in favor of modernized Functional Views.")]
public record class CreateDashboardCommand(EntityLayerType layerType, Guid layerId, string name, bool isShared = false, bool isMain = false) : IRequest<Guid>;
