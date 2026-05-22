using FluentValidation;

namespace Application;

public class CreateSpaceValidator : AbstractValidator<CreateSpaceCommand>
{
    public CreateSpaceValidator()
    {
        RuleFor(x => x.name).NotEmpty();
    }
}

