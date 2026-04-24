using Application.Common.Interfaces;
using System.Collections.Generic;
using Domain.Enums;
using MediatR;

namespace Application.Features.WorkspaceFeatures;

public record CreateWorkspaceCommand(
    string Name,
    string? Description,
    string Color,
    string Icon,
    Theme Theme,
    bool StrictJoin
) : ICommandRequest<Guid>;