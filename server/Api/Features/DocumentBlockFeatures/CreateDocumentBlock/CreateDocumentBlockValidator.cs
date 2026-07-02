using FluentValidation;

namespace Api;

public class CreateDocumentBlockValidator : AbstractValidator<CreateDocumentBlockCommand>
{
    public CreateDocumentBlockValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Block ID is required.");
        RuleFor(x => x.DocumentId).NotEmpty().WithMessage("Document ID is required.");
        RuleFor(x => x.OrderKey).NotEmpty().WithMessage("Order key is required.");
    }
}
