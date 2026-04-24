using Application.Common.Interfaces;

namespace Application.Features.WorkspaceFeatures;

public record DeleteWorkspaceCommand(Guid workspaceId) : ICommandRequest, IAuthorizedWorkspaceRequest;
