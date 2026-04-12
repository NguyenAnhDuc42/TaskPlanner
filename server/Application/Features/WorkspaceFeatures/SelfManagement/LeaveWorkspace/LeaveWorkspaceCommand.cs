using Application.Common.Interfaces;

namespace Application.Features.WorkspaceFeatures.SelfManagement.LeaveWorkspace;

public record class LeaveWorkspaceCommand(Guid WorkspaceId) : ICommandRequest, IAuthorizedWorkspaceRequest;
