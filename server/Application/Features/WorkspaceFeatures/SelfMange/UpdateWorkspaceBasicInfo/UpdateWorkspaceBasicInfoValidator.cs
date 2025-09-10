using System;
using FluentValidation;

namespace Application.Features.WorkspaceFeatures.SelfMange.UpdateWorkspaceBasicInfo;

public class UpdateWorkspaceBasicInfoValidator : AbstractValidator<UpdateWorkspaceBasicInfoCommand>
{
    public UpdateWorkspaceBasicInfoValidator()
    {
        RuleFor(x => x.name)
           .NotEmpty().WithMessage("Workspace name is required.")
           .MaximumLength(100).WithMessage("Workspace name must be at most 100 characters.");

        RuleFor(x => x.description)
            .MaximumLength(500).WithMessage("Description must be at most 500 characters.");

    }
}
