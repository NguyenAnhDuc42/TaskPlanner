using Application.Common.Interfaces;

namespace Application.Features.WorkspaceFeatures.DeleteWorkspace;

public record DeleteWorkspaceCommand(Guid workspaceId) : ICommandRequest, IAuthorizedWorkspaceRequest;
