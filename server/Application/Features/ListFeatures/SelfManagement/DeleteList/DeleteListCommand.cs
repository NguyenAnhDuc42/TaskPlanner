using System;
using MediatR;

namespace Application.Features.ListFeatures.SelfManagement.DeleteList;

public record DeleteListCommand(Guid ListId) : IRequest<Unit>;
