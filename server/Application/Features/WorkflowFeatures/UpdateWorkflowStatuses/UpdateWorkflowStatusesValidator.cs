using FluentValidation;

namespace Application.Features.WorkflowFeatures;

public class UpdateWorkflowStatusesValidator : AbstractValidator<UpdateWorkflowStatusesCommand>
{
    public UpdateWorkflowStatusesValidator()
    {
        RuleFor(x => x.WorkflowId).NotEmpty().WithMessage("Workflow ID is required.");
        RuleFor(x => x.Statuses)
            .NotNull()
            .NotEmpty().WithMessage("At least one status must be provided.");

        RuleForEach(x => x.Statuses).SetValidator(new StatusUpdateDtoValidator());
    }
}

public class StatusUpdateDtoValidator : AbstractValidator<StatusUpdateDto>
{
    public StatusUpdateDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(50).WithMessage("Name must be between 1 and 50 characters.");
        RuleFor(x => x.Color).NotEmpty().MaximumLength(20).WithMessage("Color is required.");
        RuleFor(x => x.Category).IsInEnum().WithMessage("Invalid category.");
    }
}
