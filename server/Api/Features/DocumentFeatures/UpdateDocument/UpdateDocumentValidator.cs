using FluentValidation;

namespace Api;

public class UpdateDocumentValidator : AbstractValidator<UpdateDocumentCommand>
{
    public UpdateDocumentValidator()
    {
        RuleFor(x => x.DocumentId).NotEmpty().WithMessage("Document ID is required.");
        RuleFor(x => x.Name).NotEmpty().WithMessage("Document name is required.");
    }
}
