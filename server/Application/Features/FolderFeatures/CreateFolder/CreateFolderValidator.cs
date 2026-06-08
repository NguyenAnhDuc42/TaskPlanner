using FluentValidation;

namespace Application;

public class CreateFolderValidator : AbstractValidator<CreateFolderCommand>
{
    public CreateFolderValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
    }
}

