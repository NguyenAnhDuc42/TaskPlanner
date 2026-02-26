using Application.Common.Interfaces;

namespace Application.Features.ViewFeatures.UpdateView;

public record UpdateViewCommand(
    Guid Id,
    string? Name,
    bool? IsDefault,
    string? FilterConfigJson,
    string? DisplayConfigJson
) : ICommand<MediatR.Unit>;
