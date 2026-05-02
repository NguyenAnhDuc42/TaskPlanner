using FluentValidation;

namespace Application.Features.TaskFeatures;

public class UpdateTaskValidator : AbstractValidator<UpdateTaskCommand>
{
    public UpdateTaskValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty().WithMessage("Task ID is required.");
        RuleFor(x => x.Name).MaximumLength(200).When(x => x.Name != null);
    }
}
