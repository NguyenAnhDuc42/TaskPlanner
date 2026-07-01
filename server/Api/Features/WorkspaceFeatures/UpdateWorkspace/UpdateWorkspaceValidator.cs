using FluentValidation;

namespace Api;

public class UpdateWorkspaceValidator : AbstractValidator<UpdateWorkspaceCommand>
{
    public UpdateWorkspaceValidator()
    {
        RuleFor(x => x.WorkspaceId).NotEmpty().WithMessage("Workspace ID is required.");
        RuleFor(x => x.Name).NotEmpty().When(x => x.Name != null).WithMessage("Workspace name cannot be empty.");
    }
}
