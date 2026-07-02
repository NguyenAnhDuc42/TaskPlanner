using FluentValidation;

namespace Api;

public class ReorderFavoriteValidator : AbstractValidator<ReorderFavoriteCommand>
{
    public ReorderFavoriteValidator()
    {
        RuleFor(x => x.EntityId).NotEmpty().WithMessage("Entity ID is required.");
        RuleFor(x => x.OrderKey).NotEmpty().WithMessage("Order key is required.");
    }
}
