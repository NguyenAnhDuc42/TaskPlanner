using System;
using FluentValidation;

namespace Application.Features.SpaceFeatures.CreateSpace;

public class CreateSpaceValidator : AbstractValidator<CreateSpaceCommand>
{
    public CreateSpaceValidator()
    {
        // Rule for WorkspaceId: It must not be an empty Guid.
        RuleFor(v => v.workspaceId)
            .NotEmpty().WithMessage("WorkspaceId is required.");

        // Rule for Name: It must not be empty and must be within a reasonable length.
        RuleFor(v => v.name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");

        // Rule for Description: It is optional, but if provided, it must not exceed a certain length.
        RuleFor(v => v.description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.");

        // Rule for Color: It must be a non-empty string. You could add more advanced validation
        // here to check for a valid hex code or a predefined list of colors.
        RuleFor(v => v.color)
            .NotEmpty().WithMessage("Color is required.");

        // Rule for Icon: It must be a non-empty string.
        RuleFor(v => v.icon)
            .NotEmpty().WithMessage("Icon is required.");

        // Rule for OrderKey: It must be a non-negative value.
        RuleFor(v => v.orderKey)
            .GreaterThanOrEqualTo(0).WithMessage("OrderKey must be a non-negative value.");
    }
}
