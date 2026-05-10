using FluentValidation;

namespace Application.Features.FolderFeatures;

public class MoveFolderToStatusValidator : AbstractValidator<MoveFolderToStatusCommand>
{
    public MoveFolderToStatusValidator()
    {
        RuleFor(x => x.FolderId).NotEmpty();
    }
}
