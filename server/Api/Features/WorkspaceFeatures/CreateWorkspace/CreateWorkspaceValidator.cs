using FluentValidation;

namespace Api;

public class CreateWorkspaceValidator : AbstractValidator<CreateWorkspaceCommand>
{
    public CreateWorkspaceValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Workspace ID is required.");
        RuleFor(x => x.Name).NotEmpty().WithMessage("Workspace name is required.");
    }
}
