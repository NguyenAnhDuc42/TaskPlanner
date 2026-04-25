using FluentValidation;

namespace Application.Features.FolderFeatures;

public class UpdateFolderValidator : AbstractValidator<UpdateFolderCommand>
{
    public UpdateFolderValidator()
    {
        RuleFor(x => x.FolderId)
            .NotEmpty()
            .WithMessage("Folder ID is required.");

        RuleFor(x => x.Name)
            .MaximumLength(100)
            .When(x => x.Name != null);

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .When(x => x.Description != null);
    }
}
