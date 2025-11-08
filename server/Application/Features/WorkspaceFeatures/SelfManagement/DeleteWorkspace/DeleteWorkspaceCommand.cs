using MediatR;

namespace Application.Features.WorkspaceFeatures.DeleteWorkspace;

public record class DeleteWorkspaceCommand(Guid workspaceId) : IRequest<Unit>;
