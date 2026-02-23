using Application.Common.Interfaces;
using MediatR;

namespace Application.Features.ViewFeatures.DeleteView;

public record DeleteViewCommand(Guid Id) : ICommand<Unit>;
