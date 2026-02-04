using FluentValidation;

namespace Application.Features.EntityAccessManagement.AddEntityAccess;

public class AddEntityAccessValidator : AbstractValidator<AddEntityAccessCommand>
{
    public AddEntityAccessValidator()
    {
        RuleFor(x => x.UserIds)
            .NotEmpty().WithMessage("At least one UserId is required");

        RuleFor(x => x.LayerId)
            .NotEmpty().WithMessage("LayerId is required");

        RuleFor(x => x.LayerType)
            .IsInEnum().WithMessage("Invalid LayerType");

        RuleFor(x => x.AccessLevel)
            .IsInEnum().WithMessage("Invalid AccessLevel");
    }
}
