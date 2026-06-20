using FluentValidation;

namespace Application;

public class ToggleFavoriteValidator : AbstractValidator<ToggleFavoriteCommand>
{
    public ToggleFavoriteValidator()
    {
        RuleFor(x => x.EntityId).NotEmpty();
        RuleFor(x => x.EntityLayerType).IsInEnum();
    }
}
