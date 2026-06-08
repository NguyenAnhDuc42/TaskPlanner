using FluentValidation;

namespace Application;

public class RemoveMembersValidator : AbstractValidator<RemoveMembersCommand>
{
    public RemoveMembersValidator()
    {
        RuleFor(x => x.WorkspaceId)
            .NotEmpty()
            .WithMessage("Workspace ID is required.");

        RuleFor(x => x.MemberIds)
            .NotNull()
            .Must(ids => ids is { Count: > 0 })
            .WithMessage("At least one member ID is required.");
    }
}


