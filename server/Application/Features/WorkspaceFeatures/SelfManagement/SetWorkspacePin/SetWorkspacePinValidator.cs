using FluentValidation;

namespace Application;

public class SetWorkspacePinValidator : AbstractValidator<SetWorkspacePinCommand>
{
    public SetWorkspacePinValidator()
    {
        RuleFor(x => x.WorkspaceId)
            .NotEmpty()
            .WithMessage("Workspace ID is required.");
    }
}


