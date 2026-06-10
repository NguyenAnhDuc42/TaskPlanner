using FluentValidation;

namespace Application;

public class DeleteSubTaskValidator : AbstractValidator<DeleteSubTaskCommand>
{
    public DeleteSubTaskValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty();
        RuleFor(x => x.ParentTaskId).NotEmpty();
    }
}
