using System;
using FluentValidation;

namespace Application.Features.WorkspaceFeatures.CreateWrokspace;

public class CreateWorkspaceValidator : AbstractValidator<CreateWorkspaceCommand>
{
    public CreateWorkspaceValidator()
    {
        RuleFor(x => x.name)
            .NotEmpty().WithMessage("Workspace name is required.")
            .MaximumLength(100).WithMessage("Workspace name must be at most 100 characters.");

        RuleFor(x => x.description)
            .MaximumLength(500).WithMessage("Description must be at most 500 characters.");

        RuleFor(x => x.color)
            .NotEmpty().WithMessage("Color is required.")
            .Matches("^#(?:[0-9a-fA-F]{3}){1,2}$")
            .WithMessage("Color must be a valid hex code.");

        RuleFor(x => x.icon)
            .NotEmpty().WithMessage("Icon is required.")
            .MaximumLength(50).WithMessage("Icon must be at most 50 characters.");

        RuleFor(x => x.visibility)
            .IsInEnum().WithMessage("Invalid visibility option.");
    }
}
