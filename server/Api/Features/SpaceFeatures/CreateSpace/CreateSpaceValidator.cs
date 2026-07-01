using FluentValidation;

namespace Api;

public class CreateSpaceValidator : AbstractValidator<CreateSpaceCommand>
{
    public CreateSpaceValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Space ID is required.");
        RuleFor(x => x.DefaultDocumentId).NotEmpty().WithMessage("Default document ID is required.");
        RuleFor(x => x.Name).NotEmpty().WithMessage("Space name is required.");
    }
}
