using Application.Common.Interfaces;

namespace Application.Features.WorkspaceFeatures;

public record class LeaveWorkspaceCommand(Guid WorkspaceId) : ICommandRequest, IAuthorizedWorkspaceRequest;
