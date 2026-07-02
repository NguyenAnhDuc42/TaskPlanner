using FluentValidation;

namespace Api;

public class UpdateSpaceStatusesValidator : AbstractValidator<UpdateSpaceStatusesCommand>
{
    public UpdateSpaceStatusesValidator()
    {
        RuleFor(x => x.SpaceId).NotEmpty().WithMessage("Space ID is required.");
        RuleForEach(x => x.Statuses).ChildRules(status =>
        {
            status.RuleFor(s => s.Name).NotEmpty().WithMessage("Status name is required.");
            status.RuleFor(s => s.Color).NotEmpty().WithMessage("Status color is required.");
        });
    }
}
