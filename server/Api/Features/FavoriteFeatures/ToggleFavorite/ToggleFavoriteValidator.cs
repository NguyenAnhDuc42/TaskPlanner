using FluentValidation;

namespace Api;

public class ToggleFavoriteValidator : AbstractValidator<ToggleFavoriteCommand>
{
    public ToggleFavoriteValidator()
    {
        RuleFor(x => x.EntityId).NotEmpty().WithMessage("Entity ID is required.");
        RuleFor(x => x.OrderKey).NotEmpty().WithMessage("Order key is required.");
    }
}
