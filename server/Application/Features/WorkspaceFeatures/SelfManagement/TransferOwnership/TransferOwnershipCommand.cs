using Application.Common.Interfaces;

namespace Application.Features.WorkspaceFeatures;

public record class TransferOwnershipCommand(Guid WorkspaceId, Guid NewOwnerId) : ICommandRequest, IAuthorizedWorkspaceRequest;
