using FluentValidation;

namespace Application.Features.FolderFeatures;

public class DeleteFolderValidator : AbstractValidator<DeleteFolderCommand>
{
    public DeleteFolderValidator()
    {
        RuleFor(x => x.FolderId)
            .NotEmpty()
            .WithMessage("Folder ID is required.");
    }
}
