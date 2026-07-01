using FluentValidation;

namespace Api;

public class UpdateTaskValidator : AbstractValidator<UpdateTaskCommand>
{
    public UpdateTaskValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty().WithMessage("Task ID is required.");
        RuleFor(x => x.Name).NotEmpty().When(x => x.Name != null).WithMessage("Task name cannot be empty.");
    }
}
