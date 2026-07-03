using FluentValidation;

namespace Api;

public class AddMembersValidator : AbstractValidator<AddMembersCommand>
{
    public AddMembersValidator()
    {
        RuleFor(x => x.Members).NotEmpty().WithMessage("At least one member is required.");
        RuleForEach(x => x.Members).ChildRules(member =>
        {
            member.RuleFor(m => m.Email).NotEmpty().EmailAddress().WithMessage("A valid email is required.");
        });
    }
}
