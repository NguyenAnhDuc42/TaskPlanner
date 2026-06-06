using FluentValidation;

namespace Application;

public class CreateSubTaskValidator : AbstractValidator<CreateSubTaskCommand>
{
    public CreateSubTaskValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ParentTaskId).NotEmpty();
    }
}
