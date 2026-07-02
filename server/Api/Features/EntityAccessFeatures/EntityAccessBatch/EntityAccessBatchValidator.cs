using FluentValidation;

namespace Api;

public class EntityAccessBatchValidator : AbstractValidator<EntityAccessBatchCommand>
{
    public EntityAccessBatchValidator()
    {
        RuleFor(x => x.SpaceId).NotEmpty().WithMessage("Space ID is required.");
        RuleForEach(x => x.Rows).ChildRules(row =>
        {
            row.RuleFor(r => r.MemberId).NotEmpty().WithMessage("Member ID is required.");
        });
    }
}
