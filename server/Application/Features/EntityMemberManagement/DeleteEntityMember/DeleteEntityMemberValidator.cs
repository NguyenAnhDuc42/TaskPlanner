using FluentValidation;

namespace Application.Features.EntityMemberManagement.DeleteEntityMember;

public class DeleteEntityMemberValidator : AbstractValidator<DeleteEntityMemberCommand>
{
    public DeleteEntityMemberValidator()
    {
        RuleFor(x => x.LayerId)
            .NotEmpty().WithMessage("LayerId is required");

        RuleFor(x => x.LayerType)
            .IsInEnum().WithMessage("Invalid LayerType");

        RuleFor(x => x.UserIds)
            .NotEmpty().WithMessage("At least one UserId is required");
    }
}
