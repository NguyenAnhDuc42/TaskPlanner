using FluentValidation;

namespace Application.Features.Auth.UpdateProfile;

public class UpdateProfileValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name cannot be empty.")
            .MaximumLength(100).WithMessage("Name cannot exceed 100 characters.")
            .When(x => x.Name is not null);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email cannot be empty.")
            .EmailAddress().WithMessage("Invalid email format.")
            .MaximumLength(256).WithMessage("Email cannot exceed 256 characters.")
            .When(x => x.Email is not null);

        RuleFor(x => x)
            .Must(x => x.Name is not null || x.Email is not null)
            .WithMessage("At least one field (name or email) must be provided.");
    }
}

