using System;
using System.Collections.Generic;
using System.Text;
using Application.Common.Interfaces;
using MediatR;

namespace Application.Features.DashboardManagement.EditDashboard;

public record EditDashboardCommand(Guid dashboardId, string? name = null, bool? isShared = null, bool? isMain = null) : ICommand<Unit>;