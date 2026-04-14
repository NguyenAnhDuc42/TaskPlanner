using Domain.Enums;

namespace Application.Features.WorkflowFeatures.Common;

public record StatusResponse(
    Guid Id,
    string Name,
    string Color,
    StatusCategory Category
);
