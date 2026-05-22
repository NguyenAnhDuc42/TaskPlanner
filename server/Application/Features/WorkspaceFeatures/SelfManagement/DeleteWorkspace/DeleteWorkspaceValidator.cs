using FluentValidation;

namespace Application;

public class DeleteWorkspaceValidator : AbstractValidator<DeleteWorkspaceCommand>
{
    public DeleteWorkspaceValidator()
    {
        RuleFor(x => x.workspaceId)
            .NotEmpty()
            .WithMessage("Workspace ID is required.");
    }
}


