using Application.Common.Interfaces;
using Domain.Enums;
using MediatR;

namespace Application.Features.WorkspaceFeatures.SelfManagement.TransferOwnership;

public record class TransferOwnershipCommand(Guid WorkspaceId, Guid NewOwnerId) : ICommand<Unit>;
