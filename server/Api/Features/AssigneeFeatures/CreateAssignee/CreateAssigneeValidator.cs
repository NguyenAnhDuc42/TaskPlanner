using FluentValidation;

namespace Api;

public class CreateAssigneeValidator : AbstractValidator<CreateAssigneeCommand>
{
    public CreateAssigneeValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("Assignee ID is required.");
        RuleFor(x => x.TaskId).NotEmpty().WithMessage("Task ID is required.");
        RuleFor(x => x.MemberId).NotEmpty().WithMessage("Member ID is required.");
    }
}
