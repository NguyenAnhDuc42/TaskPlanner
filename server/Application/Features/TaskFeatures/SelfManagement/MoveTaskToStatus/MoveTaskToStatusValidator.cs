using FluentValidation;

namespace Application.Features.TaskFeatures;

public class MoveTaskToStatusValidator : AbstractValidator<MoveTaskToStatusCommand>
{
    public MoveTaskToStatusValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty();
    }
}
