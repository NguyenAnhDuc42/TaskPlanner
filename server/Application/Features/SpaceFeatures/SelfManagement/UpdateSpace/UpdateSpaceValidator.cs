using FluentValidation;

namespace Application.Features.SpaceFeatures;

public class UpdateSpaceValidator : AbstractValidator<UpdateSpaceCommand>
{
    public UpdateSpaceValidator()
    {
        RuleFor(x => x.SpaceId)
            .NotEmpty()
            .WithMessage("Space ID is required.");

        RuleFor(x => x.Name)
            .MaximumLength(100)
            .When(x => x.Name != null);
    }
}
