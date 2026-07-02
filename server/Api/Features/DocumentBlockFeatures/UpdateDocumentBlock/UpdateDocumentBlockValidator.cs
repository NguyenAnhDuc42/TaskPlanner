using FluentValidation;

namespace Api;

public class UpdateDocumentBlockValidator : AbstractValidator<UpdateDocumentBlockCommand>
{
    public UpdateDocumentBlockValidator()
    {
        RuleFor(x => x.BlockId).NotEmpty().WithMessage("Block ID is required.");
        RuleFor(x => x.Content).NotEmpty().When(x => x.Content != null).WithMessage("Content cannot be empty.");
        RuleFor(x => x.OrderKey).NotEmpty().When(x => x.OrderKey != null).WithMessage("Order key cannot be empty.");
    }
}
