using FluentValidation;
using Domain.Common;
using Domain.Enums.Workspace;
using Application.Features.WorkspaceFeatures.CreateWrokspace;

namespace Application.Features.WorkspaceFeatures.CreateWorkspace;

public class CreateWorkspaceValidator : AbstractValidator<CreateWorkspaceCommand>
{
    public CreateWorkspaceValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Workspace name is required.")
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

        // Validate that the string is a valid name for the WorkspaceVariant enum
        RuleFor(x => x.Variant)
            .NotEmpty().WithMessage("Variant is required.")
            .Must(variant => Enum.IsDefined(typeof(WorkspaceVariant), variant))
            .WithMessage("Invalid workspace variant provided.");

        // Validate that the string is a valid name for the Theme enum
        RuleFor(x => x.Theme)
            .NotEmpty().WithMessage("Theme is required.")
            .Must(theme => Enum.IsDefined(typeof(Theme), theme))
            .WithMessage("Invalid theme option provided.");
    }
}