using Application.Common.Interfaces;
using MediatR;
using Domain.Enums.RelationShip;

namespace Application.Features.DashboardFeatures.DeleteDashboard;

[Obsolete("Dashboard features are legacy and will be removed in favor of modernized Functional Views.")]
public record class DeleteDashboardCommand(Guid layerId, EntityLayerType layerType, Guid dashboardId) : ICommand<Unit>;
