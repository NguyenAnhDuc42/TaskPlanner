using FluentValidation;

namespace Api;

public class DeleteAssigneeValidator : AbstractValidator<DeleteAssigneeCommand>
{
    public DeleteAssigneeValidator()
    {
        RuleFor(x => x.AssigneeId).NotEmpty().WithMessage("Assignee ID is required.");
    }
}
