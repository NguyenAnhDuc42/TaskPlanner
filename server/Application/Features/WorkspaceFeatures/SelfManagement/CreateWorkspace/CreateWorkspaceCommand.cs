using Application.Common.Interfaces;
using Application.Contract.WorkspaceContract;
using System.Collections.Generic;
using Domain.Enums;
using Domain.Enums.Workspace;
using MediatR;

namespace Application.Features.WorkspaceFeatures.SelfManagement.CreateWorkspace;

public record CreateWorkspaceCommand : ICommand<Guid>
{
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
    public string Color { get; init; } = null!;
    public string Icon { get; init; } = null!;
    public WorkspaceVariant Variant { get; init; }
    public Theme Theme { get; init; }
    public bool StrictJoin { get; init; }
}