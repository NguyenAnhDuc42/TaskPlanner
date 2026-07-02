using FluentValidation;

namespace Api;

public class DeleteWorkspaceValidator : AbstractValidator<DeleteWorkspaceCommand>
{
    public DeleteWorkspaceValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty().WithMessage("Workspace ID is required.");
    }
}
