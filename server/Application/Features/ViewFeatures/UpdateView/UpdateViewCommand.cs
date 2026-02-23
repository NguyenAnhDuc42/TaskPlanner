using Application.Common.Interfaces;

namespace Application.Features.ViewFeatures.UpdateView;

public record UpdateViewCommand(
    Guid Id,
    string Name,
    long OrderKey,
    bool IsDefault
) : ICommand<MediatR.Unit>;
