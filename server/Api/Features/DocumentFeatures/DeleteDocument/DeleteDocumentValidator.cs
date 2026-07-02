using FluentValidation;

namespace Api;

public class DeleteDocumentValidator : AbstractValidator<DeleteDocumentCommand>
{
    public DeleteDocumentValidator()
    {
        RuleFor(x => x.DocumentId).NotEmpty().WithMessage("Document ID is required.");
    }
}
