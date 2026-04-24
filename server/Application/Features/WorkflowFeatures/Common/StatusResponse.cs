using Domain.Enums;

namespace Application.Features.WorkflowFeatures;

public record StatusResponse(
    Guid Id,
    string Name,
    string Color,
    StatusCategory Category
);
