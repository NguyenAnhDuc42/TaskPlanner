using Application.Common.Interfaces;
using MediatR;

namespace Application.Features.WorkspaceFeatures.SelfManagement.LeaveWorkspace;

public record class LeaveWorkspaceCommand(Guid WorkspaceId) : ICommand<Unit>;
