using FluentValidation;

namespace Application;

public class AddMembersValidator : AbstractValidator<AddMembersCommand>
{
    public AddMembersValidator()
    {
        RuleFor(x => x.WorkspaceId)
            .NotEmpty().WithMessage("WorkspaceId is required.");
        RuleFor(x => x.Members  )
            .NotEmpty().WithMessage("At least one member must be added.")
            .Must(members => members != null && members.Count > 0)
            .WithMessage("Members list cannot be empty.")
            .Must(members => members != null && members.Count <= 100)
            .WithMessage("Cannot add more than 100 members at once.");
        RuleForEach(x => x.Members).ChildRules(member =>
        {
            member.RuleFor(m => m.Email)
                .NotEmpty().WithMessage("Email is required for each member.");
            member.RuleFor(m => m.Role)
                .IsInEnum().WithMessage("Role must be a valid enum value.");
        });
    }
}
