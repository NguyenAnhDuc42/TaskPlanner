using Application.Common.Interfaces;
using MediatR;

namespace Application.Features.WorkspaceFeatures.SelfManagement.ArchiveWorkspace;

public record class ArchiveWorkspaceCommand(Guid WorkspaceId, bool IsArchived) : ICommand<Unit>;
