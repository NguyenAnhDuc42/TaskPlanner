using Application.Common.Interfaces;
using Domain.Entities;

namespace Application.Features.SpaceFeatures;

public record GetSpaceDetailQuery(Guid SpaceId) : IQueryRequest<SpaceDetailDto>, IAuthorizedWorkspaceRequest;

public record SpaceDetailDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
    public string Color { get; init; } = null!;
    public string? Icon { get; init; }
    public bool IsPrivate { get; init; }
    public bool IsArchived { get; init; }
    public Guid? WorkflowId { get; init; }
    public Guid? StatusId { get; init; }
    public Guid DefaultDocumentId { get; init; }
    public DateTimeOffset? StartDate { get; init; }
    public DateTimeOffset? DueDate { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    
    // Pointers for local-first dictionary mapping
    public List<Guid> MemberIds { get; init; } = new();
}
