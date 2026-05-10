using Application.Common.Interfaces;

namespace Application.Features.WorkflowFeatures;

public record ReorderStatusesCommand(
    Guid StatusId,
    string? PreviousStatusOrderKey,
    string? NextStatusOrderKey,
    string? NewOrderKey = null
) : ICommandRequest, IAuthorizedWorkspaceRequest;
