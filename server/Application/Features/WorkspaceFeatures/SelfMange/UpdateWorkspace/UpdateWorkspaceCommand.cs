using Application.Common.Interfaces;
using Domain.Enums;
using Domain.Enums.Workspace;
using MediatR;

namespace Application.Features.WorkspaceFeatures.SelfMange.UpdateWorkspace;

public record UpdateWorkspaceCommand(Guid Id, string? Name, string? Description, string? Color, string? Icon, Theme? Theme, WorkspaceVariant? Variant, bool? StrictJoin, bool? IsArchived, bool RegenerateJoinCode) : ICommand<Unit>, IRequirePermission
{
    Guid? IRequirePermission.EntityId => Id;
    EntityType IRequirePermission.EntityType => EntityType.ProjectWorkspace;
    Permission IRequirePermission.RequiredPermission => Permission.Edit_Workspace;
}