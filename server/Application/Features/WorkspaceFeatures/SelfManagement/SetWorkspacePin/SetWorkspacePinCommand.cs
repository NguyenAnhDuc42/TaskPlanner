using Application.Common.Interfaces;

namespace Application.Features.WorkspaceFeatures.SelfManagement.SetWorkspacePin;

public record SetWorkspacePinCommand(Guid WorkspaceId, bool IsPinned) : ICommandRequest, IAuthorizedWorkspaceRequest;
