using FluentValidation;

namespace Api;

public class CreateFolderValidator : AbstractValidator<CreateFolderCommand>
{
    public CreateFolderValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Folder ID is required.");
        RuleFor(x => x.SpaceId).NotEmpty().WithMessage("Space ID is required.");
        RuleFor(x => x.Name).NotEmpty().WithMessage("Folder name is required.");
    }
}
