using FluentValidation;

namespace Application.Features.WorkspaceFeatures;

public class MoveItemValidator : AbstractValidator<MoveItemCommand>
{
    public MoveItemValidator()
    {
        RuleFor(x => x.ItemId)
            .NotEmpty()
            .WithMessage("Item ID is required.");

        RuleFor(x => x.ItemType)
            .IsInEnum()
            .WithMessage("Invalid item type.");

        // TargetParentId can be null (moving to root)
    }
}
