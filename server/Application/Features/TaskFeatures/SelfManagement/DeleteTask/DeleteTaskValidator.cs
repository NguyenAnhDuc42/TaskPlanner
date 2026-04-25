using FluentValidation;

namespace Application.Features.TaskFeatures;

public class DeleteTaskValidator : AbstractValidator<DeleteTaskCommand>
{
    public DeleteTaskValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty().WithMessage("Task ID is required.");
    }
}
