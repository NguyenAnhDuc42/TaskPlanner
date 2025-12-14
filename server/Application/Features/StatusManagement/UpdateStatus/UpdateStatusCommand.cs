using System;
using Domain.Enums;
using MediatR;

namespace Application.Features.StatusManagement.UpdateStatus;

public record UpdateStatusCommand(
    Guid StatusId,
    string? Name = null,
    string? Color = null,
    StatusCategory? Category = null
) : IRequest<Unit>;
