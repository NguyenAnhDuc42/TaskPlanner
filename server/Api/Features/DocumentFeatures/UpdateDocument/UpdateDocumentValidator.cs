using FluentValidation;

namespace Api;

public class UpdateDocumentValidator : AbstractValidator<UpdateDocumentCommand>
{
    public UpdateDocumentValidator()
    {
        RuleFor(x => x.Name).NotEmpty().When(x => x.Name != null).WithMessage("Document name cannot be empty.");
    }
}
