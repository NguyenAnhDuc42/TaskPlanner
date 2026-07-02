using FluentValidation;

namespace Api;

public class UpdateMembersValidator : AbstractValidator<UpdateMembersCommand>
{
    public UpdateMembersValidator()
    {
        RuleFor(x => x.Members).NotEmpty().WithMessage("At least one member update is required.");
        RuleForEach(x => x.Members).ChildRules(member =>
        {
            member.RuleFor(m => m.MemberId).NotEmpty().WithMessage("Member ID is required.");
        });
    }
}
