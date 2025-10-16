using Application.Contract.WorkspaceContract;
using Domain.Enums;
using MediatR;

namespace Application.Features.WorkspaceFeatures.CreateWrokspace;

public record CreateWorkspaceCommand(
    string Name,
    string? Description,
    string Color,
    string Icon,
    string Variant,
    string Theme,
    bool StrictJoin
) : IRequest<WorkspaceDetail>;

