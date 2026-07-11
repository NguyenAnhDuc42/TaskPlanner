using FluentValidation;

namespace Api;

public class UpdateWorkspaceStatusesValidator : AbstractValidator<UpdateWorkspaceStatusesCommand>
{
    public UpdateWorkspaceStatusesValidator()
    {
        RuleForEach(x => x.Statuses).ChildRules(status =>
        {
            status.RuleFor(s => s.Name).NotEmpty().WithMessage("Status name is required.");
            status.RuleFor(s => s.Color).NotEmpty().WithMessage("Status color is required.");
        });
    }
}
