using FluentValidation;

namespace Api;

public class UpdateSpaceValidator : AbstractValidator<UpdateSpaceCommand>
{
    public UpdateSpaceValidator()
    {
        RuleFor(x => x.SpaceId).NotEmpty().WithMessage("Space ID is required.");
        RuleFor(x => x.Name).NotEmpty().When(x => x.Name != null).WithMessage("Space name cannot be empty.");
    }
}
