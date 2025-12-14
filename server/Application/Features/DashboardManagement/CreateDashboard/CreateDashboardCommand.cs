using Application.Common.Interfaces;
using Domain.Enums.RelationShip;
using MediatR;

namespace Application.Features.DashboardManagement.CreateDashboard;


public record CreateDashboardCommand(Guid layerId,EntityLayerType layerType ,string name,bool isShared,bool isMain) : ICommand<Unit>;
