using FluentValidation;

namespace Application.Features.ViewFeatures;

public class CreateViewValidator : AbstractValidator<CreateViewCommand>
{
    public CreateViewValidator()
    {
        RuleFor(x => x.LayerId).NotEmpty();
        RuleFor(x => x.LayerType).IsInEnum();
        RuleFor(x => x.ViewType).IsInEnum();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}
