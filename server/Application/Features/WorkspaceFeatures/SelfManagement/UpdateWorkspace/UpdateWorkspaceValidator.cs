using FluentValidation;
using Application.Helpers;
using Domain.Common;

namespace Application.Features.WorkspaceFeatures;

public class UpdateWorkspaceValidator : AbstractValidator<UpdateWorkspaceCommand>
{
    public UpdateWorkspaceValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name cannot be empty if provided.")
            .MaximumLength(100).WithMessage("Name cannot exceed 100 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Name)); // Only run if Name was provided

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Description)); // Only run if Description was provided

        RuleFor(x => x.Color)
            .Must(ColorValidator.IsValidColorCode).WithMessage("Invalid color format.")
            .When(x => !string.IsNullOrWhiteSpace(x.Color)); // Only run if Color was provided

        RuleFor(x => x.Icon)
            .NotEmpty().WithMessage("Icon cannot be empty if provided.")
            .When(x => !string.IsNullOrWhiteSpace(x.Icon)); // Only run if Icon was provided
    }
}