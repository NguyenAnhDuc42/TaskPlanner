using FluentValidation;

namespace Application.Features.WorkflowFeatures;

public class ReorderStatusesValidator : AbstractValidator<ReorderStatusesCommand>
{
    public ReorderStatusesValidator()
    {
        RuleFor(x => x.StatusId).NotEmpty();
    }
}
