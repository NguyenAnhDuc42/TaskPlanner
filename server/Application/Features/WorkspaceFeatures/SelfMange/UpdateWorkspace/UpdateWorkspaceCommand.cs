using Domain.Enums.Workspace;
using MediatR;

namespace Application.Features.WorkspaceFeatures.SelfMange.UpdateWorkspace;

public record class UpdateWorkspaceCommand : IRequest<Unit>
{
    public Guid Id { get; init; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Color { get; set; }
    public string? Icon { get; set; }
    public Theme? Theme { get; set; }
    public WorkspaceVariant? Variant { get; set; }
    public bool? StrictJoin { get; set; }
    public bool? IsArchived { get; set; }
    public bool RegenerateJoinCode { get; set; }
} 
