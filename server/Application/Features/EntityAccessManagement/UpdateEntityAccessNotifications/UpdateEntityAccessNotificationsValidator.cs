using FluentValidation;

namespace Application.Features.EntityAccessManagement.UpdateEntityAccessNotifications;

public class UpdateEntityAccessNotificationsValidator : AbstractValidator<UpdateEntityAccessNotificationsCommand>
{
    public UpdateEntityAccessNotificationsValidator()
    {
        RuleFor(x => x.LayerId)
            .NotEmpty().WithMessage("LayerId is required");

        RuleFor(x => x.LayerType)
            .IsInEnum().WithMessage("Invalid LayerType");
    }
}
