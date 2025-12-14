using FluentValidation;

namespace Application.Features.EntityMemberManagement.UpdateEntityMemberNotifications;

public class UpdateEntityMemberNotificationsValidator : AbstractValidator<UpdateEntityMemberNotificationsCommand>
{
    public UpdateEntityMemberNotificationsValidator()
    {
        RuleFor(x => x.LayerId)
            .NotEmpty().WithMessage("LayerId is required");

        RuleFor(x => x.LayerType)
            .IsInEnum().WithMessage("Invalid LayerType");
    }
}
