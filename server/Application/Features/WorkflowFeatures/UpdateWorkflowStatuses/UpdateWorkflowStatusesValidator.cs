using FluentValidation;
namespace Application;

public class UpdateSpaceStatusesValidator : AbstractValidator<UpdateSpaceStatusesCommand>
{
    public UpdateSpaceStatusesValidator()
    {
        RuleFor(x => x.SpaceId).NotEmpty().WithMessage("Space ID is required.");
        RuleFor(x => x.Statuses)
            .NotNull()
            .NotEmpty().WithMessage("At least one status must be provided.");

        RuleForEach(x => x.Statuses).SetValidator(new StatusUpdateValueValidator());
    }
}

public class StatusUpdateValueValidator : AbstractValidator<StatusUpdateValue>
{
    public StatusUpdateValueValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().MaximumLength(50).WithMessage("Name must be between 1 and 50 characters.")
            .When(x => x.Action != RowAction.Delete);

        RuleFor(x => x.Color)
            .NotEmpty().MaximumLength(20).WithMessage("Color is required.")
            .When(x => x.Action != RowAction.Delete);

        RuleFor(x => x.Category)
            .IsInEnum().WithMessage("Invalid category.")
            .When(x => x.Action != RowAction.Delete);

        RuleFor(x => x.Action)
            .IsInEnum().WithMessage("Invalid action.");
    }
}
