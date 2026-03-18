using Application.Common.Interfaces;
using Domain.Enums.RelationShip;
using MediatR;
using System;

namespace Application.Features.DashboardFeatures.CreateDashboard;

public record class CreateDashboardCommand(EntityLayerType layerType, Guid layerId, string name, bool isShared = false, bool isMain = false) : ICommand<Unit>;
