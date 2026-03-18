using Application.Common.Interfaces;
using MediatR;
using Domain.Enums.RelationShip;

namespace Application.Features.DashboardFeatures.DeleteDashboard;

public record class DeleteDashboardCommand(Guid layerId, EntityLayerType layerType, Guid dashboardId) : ICommand<Unit>;
