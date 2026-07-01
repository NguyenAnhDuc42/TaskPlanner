using FluentValidation;

namespace Api;

public class UpdateFolderValidator : AbstractValidator<UpdateFolderCommand>
{
    public UpdateFolderValidator()
    {
        RuleFor(x => x.FolderId).NotEmpty().WithMessage("Folder ID is required.");
        RuleFor(x => x.Name).NotEmpty().When(x => x.Name != null).WithMessage("Folder name cannot be empty.");
    }
}
