using Domain.Enums;

namespace Application.Contract.StatusContract;

public record StatusDto(
    Guid Id,
    string Name,
    string Color,
    StatusCategory Category,
    long OrderKey,
    bool IsDefault
);
