using Application.Common.Interfaces;

namespace Application.Features.WorkspaceFeatures;

public record SetWorkspacePinCommand(Guid WorkspaceId, bool IsPinned) : ICommandRequest;
