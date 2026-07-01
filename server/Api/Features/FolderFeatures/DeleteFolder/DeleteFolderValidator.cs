using FluentValidation;

namespace Api;

public class DeleteFolderValidator : AbstractValidator<DeleteFolderCommand>
{
    public DeleteFolderValidator()
    {
        RuleFor(x => x.FolderId).NotEmpty().WithMessage("Folder ID is required.");
    }
}
