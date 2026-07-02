using FluentValidation;

namespace Api;

public class RemoveMembersValidator : AbstractValidator<RemoveMembersCommand>
{
    public RemoveMembersValidator()
    {
        RuleFor(x => x.MemberIds).NotEmpty().WithMessage("At least one member ID is required.");
    }
}
