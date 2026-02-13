using System;
using Application.Common.Interfaces;
using MediatR;

namespace Application.Features.ListFeatures.SelfManagement.DeleteList;

public record DeleteListCommand(Guid ListId) : ICommand<Unit>;
