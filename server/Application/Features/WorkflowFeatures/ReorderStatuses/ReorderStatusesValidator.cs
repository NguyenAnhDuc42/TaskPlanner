using FluentValidation;

namespace Application;

public class ReorderStatusesValidator : AbstractValidator<ReorderStatusesCommand>
{
    public ReorderStatusesValidator()
    {
        RuleFor(x => x.StatusId).NotEmpty();
    }
}

