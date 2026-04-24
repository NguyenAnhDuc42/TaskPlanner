using FluentValidation;

namespace Application.Features.WorkspaceFeatures;

public class RemoveMembersValidator : AbstractValidator<RemoveMembersCommand>
{
    public RemoveMembersValidator()
    {
        RuleFor(x => x.workspaceId)
            .NotEmpty()
            .WithMessage("Workspace ID is required.");

        RuleFor(x => x.memberIds)
            .NotNull()
            .Must(ids => ids is { Count: > 0 })
            .WithMessage("At least one member ID is required.");
    }
}

