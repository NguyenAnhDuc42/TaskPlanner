using Application.Common.Interfaces;
using MediatR;

namespace Application.Features.DashboardFeatures.EditDashboard;

public record class EditDashboardCommand(Guid dashboardId, string name, bool? isShared, bool? isMain) : ICommand<Unit>;