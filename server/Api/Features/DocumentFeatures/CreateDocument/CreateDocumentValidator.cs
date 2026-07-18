using FluentValidation;

namespace Api;

public class CreateDocumentValidator : AbstractValidator<CreateDocumentCommand>
{
    public CreateDocumentValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Document ID is required.");
        RuleFor(x => x.SpaceId).NotEmpty().WithMessage("Space ID is required.");
        RuleFor(x => x.Name).NotEmpty().WithMessage("Document name is required.");
    }
}
