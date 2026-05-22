using FluentValidation;

namespace Application;

public class CreateTaskValidator : AbstractValidator<CreateTaskCommand>
{
    public CreateTaskValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

