using FluentValidation;

namespace Application.Features.WorkflowFeatures;

public class SetLayerWorkflowValidator : AbstractValidator<SetLayerWorkflowCommand>
{
    public SetLayerWorkflowValidator()
    {
        RuleFor(x => x)
            .Must(x => x.SpaceId.HasValue || x.FolderId.HasValue)
            .WithMessage("Either SpaceId or FolderId must be provided.");

    }
}
