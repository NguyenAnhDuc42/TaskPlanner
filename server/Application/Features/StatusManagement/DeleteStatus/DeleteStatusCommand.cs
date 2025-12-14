using System;
using MediatR;

namespace Application.Features.StatusManagement.DeleteStatus;

public record DeleteStatusCommand(
    Guid StatusId
) : IRequest<Unit>;
