using FluentValidation;

namespace Application;

public class CreateFolderValidator : AbstractValidator<CreateFolderCommand>
{
    public CreateFolderValidator()
    {
        RuleFor(x => x.name).NotEmpty();
    }
}

