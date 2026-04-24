using FluentValidation;

namespace Application.Features.FolderFeatures;

public class CreateFolderValidator : AbstractValidator<CreateFolderCommand>
{
    public CreateFolderValidator()
    {
        RuleFor(x => x.name).NotEmpty();
    }
}
