using Application.Common.Interfaces;
using Domain.Enums;

namespace Application.Features.WorkflowFeatures;

public record GetWorkspaceWorkflowsQuery() : IQueryRequest<List<WorkflowDto>>, IAuthorizedWorkspaceRequest;

public record WorkflowDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;
    public Guid? SpaceId { get; init; }
    public Guid? FolderId { get; init; }
    public List<StatusDto> Statuses { get; init; } = new();
}

public record StatusDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;
    public string Color { get; init; } = null!;
    public StatusCategory Category { get; init; }
}
