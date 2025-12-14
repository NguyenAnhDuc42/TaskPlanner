using Application.Common.Interfaces;
using Domain.Enums.RelationShip;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Features.DashboardManagement.DeleteDashboard;

public record class DeleteDashboardCommand(Guid layerId,EntityLayerType layerType,Guid dashboardId) : ICommand<Unit>;

