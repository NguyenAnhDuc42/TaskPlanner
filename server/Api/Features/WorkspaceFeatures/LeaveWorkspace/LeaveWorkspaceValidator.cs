using FluentValidation;

namespace Api;

public class LeaveWorkspaceValidator : AbstractValidator<LeaveWorkspaceCommand>
{
    public LeaveWorkspaceValidator()
    {
        RuleFor(x => x.WorkspaceId)
            .NotEmpty()
            .WithMessage("Workspace ID is required.");
    }
}
