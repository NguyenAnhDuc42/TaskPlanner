using FluentValidation;

namespace Api;

public class DeleteDocumentBlockValidator : AbstractValidator<DeleteDocumentBlockCommand>
{
    public DeleteDocumentBlockValidator()
    {
        RuleFor(x => x.BlockId).NotEmpty().WithMessage("Block ID is required.");
    }
}
