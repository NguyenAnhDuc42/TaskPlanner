using Application.Common.Interfaces;

namespace Application.Features.WorkspaceFeatures.SelfManagement.TransferOwnership;

public record class TransferOwnershipCommand(Guid WorkspaceId, Guid NewOwnerId) : ICommandRequest, IAuthorizedWorkspaceRequest;
