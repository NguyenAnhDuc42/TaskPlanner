using FluentValidation;

namespace Application.Features.WorkspaceFeatures.SelfManagement.JoinWorkspaceByCode;

public class JoinWorkspaceByCodeValidator : AbstractValidator<JoinWorkspaceByCodeCommand>
{
    public JoinWorkspaceByCodeValidator()
    {
        RuleFor(x => x.JoinCode)
            .NotEmpty().WithMessage("Join code is required.")
            .MaximumLength(32).WithMessage("Join code is too long.");
    }
}

