using Application.Common.Interfaces;

namespace Application.Features.ViewFeatures.DeleteView;

public record DeleteViewCommand(Guid Id) : ICommandRequest;
