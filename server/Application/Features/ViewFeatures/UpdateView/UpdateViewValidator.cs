using FluentValidation;

namespace Application.Features.ViewFeatures;

public class UpdateViewValidator : AbstractValidator<UpdateViewCommand>
{
    public UpdateViewValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100)
            .When(x => x.Name != null);
    }
}
