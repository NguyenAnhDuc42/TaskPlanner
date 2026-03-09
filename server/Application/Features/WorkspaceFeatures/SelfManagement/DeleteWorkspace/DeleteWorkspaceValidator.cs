using FluentValidation;

namespace Application.Features.WorkspaceFeatures.DeleteWorkspace;

public class DeleteWorkspaceValidator : AbstractValidator<DeleteWorkspaceCommand>
{
    public DeleteWorkspaceValidator()
    {
        RuleFor(x => x.workspaceId)
            .NotEmpty()
            .WithMessage("Workspace ID is required.");
    }
}

