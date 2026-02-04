using FluentValidation;

namespace Application.Features.FolderFeatures.SelfManagement.CreateFolder;

public class CreateFolderValidator : AbstractValidator<CreateFolderCommand>
{
    public CreateFolderValidator()
    {
        RuleFor(x => x.name).NotEmpty();
    }
}
