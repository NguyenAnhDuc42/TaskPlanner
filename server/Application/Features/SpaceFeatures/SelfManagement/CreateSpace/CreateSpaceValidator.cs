using FluentValidation;

namespace Application.Features.SpaceFeatures.SelfManagement.CreateSpace;

public class CreateSpaceValidator : AbstractValidator<CreateSpaceCommand>
{
    public CreateSpaceValidator()
    {
        RuleFor(x => x.name).NotEmpty();
    }
}
