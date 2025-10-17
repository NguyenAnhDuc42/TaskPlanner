using FluentValidation;
using Domain.Enums.Workspace;
using Domain.Common; // Assuming ColorValidator is here
namespace Application.Features.WorkspaceFeatures.SelfMange.UpdateWorkspace;

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

        RuleFor(x => x)
            .Must(command => command.Variant != WorkspaceVariant.Personal || !command.StrictJoin.HasValue || command.StrictJoin.Value == false)
            .WithMessage("A Personal workspace cannot have strict join enabled.")
            .When(x => x.Variant.HasValue && x.StrictJoin.HasValue); // Only run if both were provided

        RuleFor(x => x)
            .Must(command => !command.IsArchived.HasValue || !command.IsArchived.Value || command.Variant != WorkspaceVariant.Company)
            .WithMessage("A Company workspace cannot be archived via this endpoint. Please contact support.")
            .When(x => x.IsArchived.HasValue && x.Variant.HasValue);
    }
}