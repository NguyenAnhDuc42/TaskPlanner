using System;
using FluentValidation;

namespace Application.Features.SpaceFeatures.UpdateSpaceBasicInfo;

public class UpdateSpaceBasicInfoValidator : AbstractValidator<UpdateSpaceBasicInfoCommand>
{
    public UpdateSpaceBasicInfoValidator()
    {
        // Rule for SpaceId: It must not be an empty Guid.
        RuleFor(v => v.spaceId)
            .NotEmpty().WithMessage("SpaceId is required.");

        // Rule for Name: It must not be empty and must be within a reasonable length.
        RuleFor(v => v.name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");

        // Rule for Description: It is optional, but if provided, it must not exceed a certain length.
        RuleFor(v => v.description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.");
    }
}