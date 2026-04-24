using FluentValidation;

namespace Application.Features.WorkspaceFeatures;

public class SetWorkspacePinValidator : AbstractValidator<SetWorkspacePinCommand>
{
    public SetWorkspacePinValidator()
    {
        RuleFor(x => x.WorkspaceId)
            .NotEmpty()
            .WithMessage("Workspace ID is required.");
    }
}

