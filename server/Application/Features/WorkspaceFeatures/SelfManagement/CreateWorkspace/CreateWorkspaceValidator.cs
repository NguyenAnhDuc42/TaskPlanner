using FluentValidation;
using Domain.Common;
using Domain.Enums.Workspace;

namespace Application.Features.WorkspaceFeatures.SelfManagement.CreateWorkspace;

public class CreateWorkspaceValidator : AbstractValidator<CreateWorkspaceCommand>
{
    public CreateWorkspaceValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Workspace name is required.")
            .MinimumLength(2).WithMessage("Workspace name must be at least 2 characters.")
            .MaximumLength(100).WithMessage("Workspace name must be at most 100 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must be at most 500 characters.");

        RuleFor(x => x.Color)
            .NotEmpty().WithMessage("Color is required.")
            .Must(ColorValidator.IsValidColorCode)
            .WithMessage("Color must be a valid hex code.");

        RuleFor(x => x.Icon)
            .NotEmpty().WithMessage("Icon is required.")
            .MaximumLength(50).WithMessage("Icon must be at most 50 characters.");

        // Validate that the value is a valid member of the WorkspaceVariant enum
        RuleFor(x => x.Variant)
            .Must(variant => Enum.IsDefined(typeof(WorkspaceVariant), variant))
            .WithMessage("Invalid workspace variant provided.");

        // Validate that the value is a valid member of the Theme enum
        RuleFor(x => x.Theme)
            .Must(theme => Enum.IsDefined(typeof(Theme), theme))
            .WithMessage("Invalid theme option provided.");
    }
}