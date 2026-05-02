using FluentValidation;

namespace Application.Features.TaskFeatures;

public class CreateTaskValidator : AbstractValidator<CreateTaskCommand>
{
    public CreateTaskValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}
