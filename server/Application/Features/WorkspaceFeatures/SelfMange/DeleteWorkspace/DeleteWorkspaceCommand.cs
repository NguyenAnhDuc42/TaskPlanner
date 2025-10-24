using MediatR;

namespace Application.Features.WorkspaceFeatures.SelfMange.DeleteWorkspace;

public record class DeleteWorkspaceCommand(Guid workspaceId) : IRequest<Unit>;
