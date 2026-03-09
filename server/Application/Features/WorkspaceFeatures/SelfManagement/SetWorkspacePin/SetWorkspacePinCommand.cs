using Application.Common.Interfaces;
using MediatR;

namespace Application.Features.WorkspaceFeatures.SelfManagement.SetWorkspacePin;

public record SetWorkspacePinCommand(Guid WorkspaceId, bool IsPinned) : ICommand<Unit>;

