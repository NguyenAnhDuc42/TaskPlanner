using FluentValidation;

namespace Application;

public class UpdateSubTaskValidator : AbstractValidator<UpdateSubTaskCommand>
{
    public UpdateSubTaskValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty();
        RuleFor(x => x.ParentTaskId).NotEmpty();
        RuleFor(x => x.SpaceId).NotEmpty();
        RuleFor(x => x.Name).MaximumLength(200).When(x => x.Name != null);
    }
}
