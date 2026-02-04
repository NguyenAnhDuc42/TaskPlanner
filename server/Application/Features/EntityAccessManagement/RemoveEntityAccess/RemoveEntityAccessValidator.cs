using FluentValidation;

namespace Application.Features.EntityAccessManagement.RemoveEntityAccess;

public class RemoveEntityAccessValidator : AbstractValidator<RemoveEntityAccessCommand>
{
    public RemoveEntityAccessValidator()
    {
        RuleFor(x => x.LayerId)
            .NotEmpty().WithMessage("LayerId is required");

        RuleFor(x => x.LayerType)
            .IsInEnum().WithMessage("Invalid LayerType");

        RuleFor(x => x.UserIds)
            .NotEmpty().WithMessage("At least one UserId is required");
    }
}
