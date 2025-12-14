using System;
using MediatR;

namespace Application.Features.StatusManagement.ReorderStatus;

public record ReorderStatusCommand(
    Guid StatusId,
    long NewOrderKey
) : IRequest<Unit>;
