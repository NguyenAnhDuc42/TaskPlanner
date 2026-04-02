using Domain.Enums;

namespace Application.Contract.Common;

public record DocumentDto(
    Guid Id,
    Guid LayerId,
    string Name,
    string Content
);
