using Application.Common.Interfaces;
using Domain.Enums;
using MediatR;

namespace Application.Features.WorkspaceFeatures.SelfMange.DeleteWorkspace;

public record class DeleteWorkspaceCommand(Guid workspaceId) : ICommand<Guid>, IRequirePermission
{
    public Guid? EntityId => workspaceId;
    public EntityType EntityType => EntityType.ProjectWorkspace;
    public Permission RequiredPermission => Permission.Delete_Workspace;
}
