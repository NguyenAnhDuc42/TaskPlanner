using FluentValidation;

namespace Application.Features.SpaceFeatures;

public class CreateSpaceValidator : AbstractValidator<CreateSpaceCommand>
{
    public CreateSpaceValidator()
    {
        RuleFor(x => x.name).NotEmpty();
    }
}
